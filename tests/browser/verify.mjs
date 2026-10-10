// Browser verification for conclusions HtmlRenderer cannot confirm without a
// real engine. The original three ninth-review checks remain the first group:
//   REV-121  theme transition timing derived from tokens, collapsing under
//            prefers-reduced-motion
//   REV-118  focus ring still painted after the alias was removed
//   REV-123  roving focus still works after ToggleGroup/Toolbar were merged
import { connect, makeHelpers } from './cdp.mjs';

const BASE = process.env.AETERNI_BASE_URL ?? 'http://127.0.0.1:5099';

const { send, once, close } = await connect();
await send('Page.enable');
await send('Runtime.enable');
const { evaluate, goto, waitForApp, key, setReducedMotion } = makeHelpers({ send, once });

const results = [];
const check = (name, ok, detail) => {
    results.push({ name, ok, detail });
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? ` — ${detail}` : ''}`);
};

const sleep = ms => new Promise(r => setTimeout(r, ms));

// ---------------------------------------------------------------- REV-121
await goto(`${BASE}/components/theme`);
await waitForApp();

// The ThemeProvider writes `data-theme` on <html> through the interactive runtime,
// so its presence proves the wasm runtime is live and the switch's handlers are
// wired. `waitForApp` only proves the markup rendered, and a click before that
// point is silently dropped (it was the source of this check's flakiness).
async function waitForInteractiveTheme(selector = '[data-theme]') {
    const deadline = Date.now() + 60000;
    while (Date.now() < deadline) {
        if (await evaluate(`!!document.querySelector('${selector}')`)) return true;
        await sleep(200);
    }
    return false;
}

const themeReady = await waitForInteractiveTheme();

// The sample's ThemeSwitch is the radiogroup labelled "主题模式"; option 2 is Dark.
const pick = index => evaluate(`(() => {
    const group = [...document.querySelectorAll('[role="radiogroup"]')]
        .find(g => g.querySelectorAll('input[type=radio]').length === 3 && /主题|Theme/.test(g.getAttribute('aria-label') || ''));
    const input = group.querySelectorAll('input[type=radio]')[${index}];
    input.click();
    return group.getAttribute('aria-label');
})()`);

// Measure how long the transition class stays on <html>.
//
// The timestamps are taken in the page, not by polling from here: every poll is
// a CDP round trip, and on a loaded CI runner that latency dwarfed the duration
// being measured (620ms was reported as 1000ms). The observer records the add
// and the remove with performance.now(), so the number is the duration the
// browser actually applied.
async function measureTransition(optionIndex) {
    // Blazor WebAssembly attaches its handlers asynchronously: `waitForApp` only
    // proves the markup rendered, so the very first click can land before the
    // interactive runtime is live and silently do nothing. An unhandled click
    // cannot change the theme, so retrying the same option is safe and the loop
    // only exits once the browser has actually seen the transition class.
    const deadline = Date.now() + 30000;
    while (Date.now() < deadline) {
        await evaluate(`(() => {
            window.__obs && window.__obs.disconnect();
            window.__seen = false; window.__t0 = null; window.__t1 = null;
            window.__obs = new MutationObserver(() => {
                const on = document.documentElement.classList.contains('aeterni-theme-transitioning');
                if (on) { window.__seen = true; if (window.__t0 === null) window.__t0 = performance.now(); }
                else if (window.__t0 !== null && window.__t1 === null) { window.__t1 = performance.now(); }
            });
            window.__obs.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
        })()`);
        await pick(optionIndex);
        const appliedImmediately = await evaluate(`document.documentElement.classList.contains('aeterni-theme-transitioning')`);
        const classSeen = appliedImmediately || await (async () => {
            const waitUntil = Date.now() + 1500;
            while (Date.now() < waitUntil) {
                if (await evaluate('window.__seen')) return true;
                await sleep(25);
            }
            return false;
        })();
        if (!classSeen) {
            await sleep(300);
            continue;
        }

        const endOfCycle = Date.now() + 5000;
        while (Date.now() < endOfCycle) {
            if (await evaluate('window.__t1 !== null')) break;
            await sleep(25);
        }
        const observed = await evaluate(`JSON.stringify({ seen: window.__seen, t0: window.__t0, t1: window.__t1 })`);
        const { seen, t0, t1 } = JSON.parse(observed);
        return { appliedImmediately, sawClass: seen, removedAfter: t0 !== null && t1 !== null ? Math.round(t1 - t0) : null };
    }

    return { appliedImmediately: false, sawClass: false, removedAfter: null };
}

const normal = await measureTransition(2);
const themeDiag = await evaluate(`JSON.stringify((() => {
    const group = [...document.querySelectorAll('[role="radiogroup"]')]
        .find(g => g.querySelectorAll('input[type=radio]').length === 3 && /主题|Theme/.test(g.getAttribute('aria-label') || ''));
    const options = group ? [...group.querySelectorAll('input[type=radio]')] : [];
    return {
        groupLabel: group ? group.getAttribute('aria-label') : null,
        optionLabels: options.map(o => o.getAttribute('aria-label')),
        checked: options.findIndex(o => o.checked),
        reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
        prefersDark: matchMedia('(prefers-color-scheme: dark)').matches,
        htmlClass: document.documentElement.className,
        blazor: typeof window.Blazor,
        dataTheme: document.documentElement.getAttribute('data-theme')
    };
})())`);
check('REV-121 the transition class is applied on a real theme switch', normal.appliedImmediately && normal.sawClass,
    `applied=${normal.appliedImmediately} observed=${normal.sawClass} ready=${themeReady} diag=${themeDiag}`);
check('REV-121 it is removed once the token-derived duration elapses (~620ms)',
    normal.removedAfter !== null && normal.removedAfter >= 550 && normal.removedAfter <= 750,
    `removed after ${normal.removedAfter}ms`);

await setReducedMotion(true);
const reduced = await measureTransition(1);
check('REV-121 reduced motion collapses the wait instead of blocking for the full duration',
    reduced.removedAfter !== null && reduced.removedAfter <= 200,
    `removed after ${reduced.removedAfter}ms (normal was ${normal.removedAfter}ms)`);
await setReducedMotion(false);

// Wait until the component's JS module has synced: the roving helper installs
// exactly one tab stop on its first refresh, so that is the ready signal.
async function waitForRoving(host, item) {
    const deadline = Date.now() + 5000;
    while (Date.now() < deadline) {
        const ready = await evaluate(`(() => {
            const host = document.querySelector('${host}');
            if (!host) return false;
            const items = [...host.querySelectorAll('${item}')].filter(b => !b.disabled && b.getClientRects().length);
            return items.filter(b => b.getAttribute('tabindex') === '0').length === 1;
        })()`);
        if (ready) return true;
        await sleep(100);
    }
    return false;
}

// Real keyboard focus, so :focus-visible matches instead of a programmatic .focus().
async function tabUntil(selector, maxPresses = 40) {
    await evaluate('document.activeElement && document.activeElement.blur(); document.body.focus();');
    for (let i = 0; i < maxPresses; i++) {
        await key('Tab', 'Tab', 9);
        await sleep(20);
        const hit = await evaluate(`!!document.activeElement && document.activeElement.matches('${selector}')`);
        if (hit) return true;
    }
    return false;
}

// ---------------------------------------------------------------- REV-118
await goto(`${BASE}/components/theme`);
await waitForApp();
const reached = await tabUntil('[role="combobox"]');
const ring = await evaluate(`(() => {
    const trigger = document.querySelector('[role="combobox"]');
    const style = getComputedStyle(trigger);
    return {
        outlineWidth: style.outlineWidth,
        outlineColor: style.outlineColor,
        outlineStyle: style.outlineStyle,
        reached: __REACHED__,
        rootAlias: getComputedStyle(document.documentElement).getPropertyValue('--aeterni-control-focus-ring').trim(),
        focusColor: getComputedStyle(document.documentElement).getPropertyValue('--aeterni-focus-color').trim()
    };
})()`.replace('__REACHED__', String(reached)));
check('REV-118 a repointed consumer still paints a focus ring',
    reached && parseFloat(ring.outlineWidth) > 0 && ring.outlineStyle !== 'none' && !/rgba?\(0, 0, 0, 0\)/.test(ring.outlineColor),
    `width=${ring.outlineWidth} style=${ring.outlineStyle} color=${ring.outlineColor}`);
check('REV-118 the removed alias resolves to nothing', ring.rootAlias === '', `alias="${ring.rootAlias}"`);

// The inset ring (Menu, MultiSelect options) is NOT covered here. Reaching it
// means putting keyboard focus inside an open overlay, and the trigger contract
// only promises Tab plus Enter/Space on the trigger — it does not promise that
// ArrowDown enters the menu, and it does not. What the browser does confirm is
// that the token the inset depends on resolves:
const focusWidth = await evaluate(
    `getComputedStyle(document.documentElement).getPropertyValue('--aeterni-focus-width').trim()`);
check('REV-118 the focus width the inset ring is derived from resolves',
    focusWidth === '3px', `--aeterni-focus-width=${focusWidth}`);
console.log('NOTE  the inset ring itself is covered by the contract assertion on the\n' +
            '      declared outline-offset, not by this harness.');

// ---------------------------------------------------------------- REV-123
for (const [path, host, item] of [
    ['/components/toolbar', '[role="toolbar"]', 'button'],
    ['/components/toggle-group', '.aeterni-toggle-group', '.aeterni-toggle-group__item']
]) {
    await goto(`${BASE}${path}`);
    await waitForApp();
    const rovingReady = await waitForRoving(host, item);
    const state = await evaluate(`(() => {
        const host = document.querySelector('${host}');
        const items = [...host.querySelectorAll('${item}')].filter(b => !b.disabled && b.getClientRects().length);
        if (items.length < 3) return { error: 'not enough items: ' + items.length };
        items[0].focus();
        return {
            count: items.length,
            tabindex: items.map(b => b.getAttribute('tabindex')),
            focusedIndex: items.indexOf(document.activeElement)
        };
    })()`);
    if (state.error) { check(`REV-123 ${path} roving focus`, false, state.error); continue; }
    check(`REV-123 ${path} roving module synced`, rovingReady, rovingReady ? '' : 'no tab stop appeared');
    check(`REV-123 ${path} keeps a single tab stop`, state.tabindex.filter(t => t === '0').length === 1,
        `tabindex=${JSON.stringify(state.tabindex)}`);

    await key('ArrowRight', 'ArrowRight', 39);
    await sleep(50);
    const after = await evaluate(`(() => {
        const host = document.querySelector('${host}');
        const items = [...host.querySelectorAll('${item}')].filter(b => !b.disabled && b.getClientRects().length);
        return { index: items.indexOf(document.activeElement), tabindex: items.map(b => b.getAttribute('tabindex')) };
    })()`);
    check(`REV-123 ${path} ArrowRight moves focus to the next item`,
        after.index === state.focusedIndex + 1,
        `focused ${state.focusedIndex} -> ${after.index}`);
    check(`REV-123 ${path} moves the tab stop with it`, after.tabindex.filter(t => t === '0').length === 1,
        `tabindex=${JSON.stringify(after.tabindex)}`);
}

// wrapping from the first item backwards lands on the last
await goto(`${BASE}/components/toggle-group`);
await waitForApp();
await waitForRoving('.aeterni-toggle-group', '.aeterni-toggle-group__item');
// Scope to one host: the page shows several toggle groups, and the ring wraps
// inside a group, not across the page.
await evaluate(`document.querySelector('.aeterni-toggle-group .aeterni-toggle-group__item').focus()`);
await key('ArrowLeft', 'ArrowLeft', 37);
await sleep(50);
const wrapped = await evaluate(`(() => {
    const host = document.querySelector('.aeterni-toggle-group');
    const items = [...host.querySelectorAll('.aeterni-toggle-group__item')].filter(b => !b.disabled && b.getClientRects().length);
    return { index: items.indexOf(document.activeElement), last: items.length - 1, count: items.length };
})()`);
check('REV-123 ArrowLeft wraps from the first item to the last', wrapped.index === wrapped.last,
    `focused ${wrapped.index} of ${wrapped.last} (${wrapped.count} items in the group)`);

// ---------------------------------------------------------------- REV-98
// The notice region must sit in the page's root stacking context, not behind a
// provider-root context capped at the backdrop rung.
await goto(`${BASE}/components/feedback`);
await waitForApp();
await evaluate(`[...document.querySelectorAll('button')].find(b => b.textContent.trim() === 'Toast').click()`);
await sleep(600);
const stacking = await evaluate(`(() => {
    const provider = document.querySelector('.aeterni-dialog-provider');
    const region = document.querySelector('.aeterni-dialog-provider__notice-region');
    if (!provider || !region) return { error: 'no live notice region' };
    const style = getComputedStyle(provider);
    let capped = null;
    for (let n = region.parentElement; n && n !== document.documentElement; n = n.parentElement) {
        const s = getComputedStyle(n);
        const creates = (s.position !== 'static' && s.zIndex !== 'auto') || s.isolation === 'isolate';
        if (creates) capped = { node: n.className, z: s.zIndex };
    }
    return {
        providerPosition: style.position,
        providerZ: style.zIndex,
        providerIsolation: style.isolation,
        regionZ: getComputedStyle(region).zIndex,
        capped
    };
})()`);
check('REV-98 the provider root is no longer a stacking context',
    stacking.providerPosition === 'static' && stacking.providerZ === 'auto' && stacking.providerIsolation === 'auto',
    `position=${stacking.providerPosition} z=${stacking.providerZ} isolation=${stacking.providerIsolation}`);
check('REV-98 nothing caps the notice region below the overlay rungs',
    stacking.capped === null && Number(stacking.regionZ) > 1060,
    stacking.capped ? `capped by ${stacking.capped.node} at ${stacking.capped.z}` : `region z=${stacking.regionZ}, popover=1060 tooltip=1070`);

// ---------------------------------------------------------------- Priority 7
// The two conclusions HtmlRenderer cannot see: the Slider paints its focus ring
// on the thumb (a computed-style question) and the new key guards stop the
// document from acting on PageUp/PageDown while the value changes (a default-action
// question). The pointer drag is checked here as well because it is a real
// browser interaction from end to end.

async function mouse(type, x, y, extra = {}) {
    await send('Input.dispatchMouseEvent', { type, x, y, button: 'left', clickCount: 1, ...extra });
}

await goto(`${BASE}/components/slider`);
await waitForApp();

// The route is served from the published bundle, so a stale dist/ would land
// here without the component; fail the wait rather than the query below.
const sliderDeadline = Date.now() + 10000;
while (Date.now() < sliderDeadline) {
    if (await evaluate(`!!document.querySelector('.aeterni-slider__track')`)) break;
    await sleep(100);
}

// Drag-driven readiness: the module binds its pointer handler after the first
// sync, and the drag end-to-end is itself the assertion, so a retry loop both
// waits for the module and proves the geometry. 0.75 of the sample's continuous
// 0..100 track is exactly 75.
async function dragTo(fraction) {
    const rect = await evaluate(`(() => {
        const track = document.querySelector('.aeterni-slider__track');
        track.scrollIntoView({ block: 'center' });
        const r = track.getBoundingClientRect();
        return { left: r.left, top: r.top, width: r.width, height: r.height };
    })()`);
    const x = rect.left + rect.width * fraction;
    const y = rect.top + rect.height / 2;
    await mouse('mousePressed', x, y, { buttons: 1 });
    await mouse('mouseMoved', x, y, { buttons: 1 });
    await mouse('mouseReleased', x, y, { buttons: 0 });
}

let sliderValue = '40.00';
const dragDeadline = Date.now() + 5000;
while (Date.now() < dragDeadline) {
    await dragTo(0.75);
    await sleep(120);
    sliderValue = await evaluate(`document.querySelector('[role="slider"]').getAttribute('aria-valuenow')`);
    if (sliderValue === '75.00') break;
    await sleep(200);
}
check('P7 dragging the continuous track commits the pointer value without snapping', sliderValue === '75.00',
    `aria-valuenow=${sliderValue}`);
check('P7 the drag left no is-dragging state behind',
    await evaluate(`!document.querySelector('[role="slider"]').classList.contains('is-dragging')`));

// Real Tab so :focus-visible matches; the sidebar is long, so allow room.
const sliderReached = await tabUntil('[role="slider"]', 80);
const thumbRing = await evaluate(`(() => {
    const style = getComputedStyle(document.querySelector('.aeterni-slider__thumb'));
    return { width: style.outlineWidth, style: style.outlineStyle, color: style.outlineColor };
})()`);
check('P7 keyboard focus paints the ring on the thumb', sliderReached && parseFloat(thumbRing.width) > 0
    && thumbRing.style !== 'none' && !/rgba?\(0, 0, 0, 0\)/.test(thumbRing.color),
    `reached=${sliderReached} width=${thumbRing.width} style=${thumbRing.style} color=${thumbRing.color}`);

// Tab-into-view scrolling can still be animating when the ring check returns;
// two identical reads 120ms apart is a settled baseline.
async function waitForScrollSettle() {
    let last = -1;
    for (let i = 0; i < 20; i++) {
        const y = await evaluate('window.scrollY');
        if (y === last) return y;
        last = y;
        await sleep(120);
    }
    return last;
}

const scrollTopBefore = await waitForScrollSettle();
await key('PageDown', 'PageDown', 34);
await sleep(120);
const sliderAfterKeys = await evaluate(`({
    value: document.querySelector('[role="slider"]').getAttribute('aria-valuenow'),
    scrollY: window.scrollY,
    scrollable: document.documentElement.scrollHeight > window.innerHeight
})`);
check('P7 PageDown adjusts the continuous slider without scrolling the page',
    sliderAfterKeys.value === '65.00' && sliderAfterKeys.scrollable && sliderAfterKeys.scrollY === scrollTopBefore,
    `value 75.00 -> ${sliderAfterKeys.value}, scrollY ${scrollTopBefore} -> ${sliderAfterKeys.scrollY} (scrollable=${sliderAfterKeys.scrollable})`);

await goto(`${BASE}/components/input-number`);
await waitForApp();
const numberBefore = await evaluate(`(() => {
    const input = document.querySelector('input[role="spinbutton"]');
    input.focus({ preventScroll: true });
    return { value: input.getAttribute('aria-valuenow'), scrollY: window.scrollY,
        scrollable: document.documentElement.scrollHeight > window.innerHeight };
})()`);
await key('ArrowUp', 'ArrowUp', 38);
await sleep(80);
const numberUp = await evaluate(`document.querySelector('input[role="spinbutton"]').getAttribute('aria-valuenow')`);
const scrollAfterUp = await evaluate('window.scrollY');
await key('PageDown', 'PageDown', 34);
await sleep(120);
const numberDown = await evaluate(`document.querySelector('input[role="spinbutton"]').getAttribute('aria-valuenow')`);
const scrollAfterPageDown = await evaluate('window.scrollY');
check('P7 ArrowUp steps the number field once', numberUp === '13.0' && numberBefore.value === '12.5',
    `${numberBefore.value} -> ${numberUp}`);
check('P7 PageDown steps ten increments without scrolling the page',
    numberDown === '8.0' && numberBefore.scrollable && scrollAfterUp === numberBefore.scrollY
        && scrollAfterPageDown === numberBefore.scrollY,
    `value ${numberUp} -> ${numberDown}, scrollY ${numberBefore.scrollY} -> ${scrollAfterUp} -> ${scrollAfterPageDown} (scrollable=${numberBefore.scrollable})`);

// ---------------------------------------------------------- Timeline / Stepper
// These are layout conclusions: HtmlRenderer can see the ordered-list contract,
// but only a real engine can prove the SVG is centred, the axis actually turns,
// and the homepage contains its long history inside the viewport.
await goto(`${BASE}/components/timeline`);
await waitForApp();
const timelineDeadline = Date.now() + 5000;
while (Date.now() < timelineDeadline) {
    if (await evaluate(`!!document.querySelector('.aeterni-timeline__marker .aeterni-icon')`)) break;
    await sleep(100);
}

const timelineIcon = await evaluate(`(() => {
    const icon = document.querySelector('.aeterni-timeline__marker .aeterni-icon');
    const marker = icon && icon.closest('.aeterni-timeline__marker');
    if (!icon || !marker) return { error: 'icon marker not rendered' };
    const i = icon.getBoundingClientRect();
    const m = marker.getBoundingClientRect();
    return {
        x: Math.abs((i.left + i.width / 2) - (m.left + m.width / 2)),
        y: Math.abs((i.top + i.height / 2) - (m.top + m.height / 2)),
        markerDisplay: getComputedStyle(marker).display
    };
})()`);
check('Timeline centres marker icons on both axes',
    !timelineIcon.error && timelineIcon.x <= 0.5 && timelineIcon.y <= 0.5,
    timelineIcon.error ?? `delta=(${timelineIcon.x.toFixed(2)}, ${timelineIcon.y.toFixed(2)}) display=${timelineIcon.markerDisplay}`);

await evaluate(`(() => {
    const label = [...document.querySelectorAll('label')].find(node => node.textContent.includes('横向布局'));
    label.querySelector('input[type=checkbox]').click();
})()`);
await sleep(100);
const horizontalTimeline = await evaluate(`(() => {
    const root = document.querySelector('.aeterni-timeline');
    const markers = [...root.querySelectorAll('.aeterni-timeline__marker')].map(node => node.getBoundingClientRect());
    const connector = getComputedStyle(root.querySelector('.aeterni-timeline__item'), '::after');
    return {
        horizontal: root.classList.contains('aeterni-timeline--horizontal'),
        overflowX: getComputedStyle(root).overflowX,
        sameAxis: markers.every(rect => Math.abs((rect.top + rect.height / 2) - (markers[0].top + markers[0].height / 2)) <= 0.5),
        advances: markers.every((rect, index) => index === 0 || rect.left > markers[index - 1].left),
        connectorWidth: parseFloat(connector.width),
        pageOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
    };
})()`);
check('Timeline horizontal mode turns the marker axis and contains overflow',
    horizontalTimeline.horizontal && horizontalTimeline.overflowX === 'auto'
        && horizontalTimeline.sameAxis && horizontalTimeline.advances
        && horizontalTimeline.connectorWidth > 0 && horizontalTimeline.pageOverflow <= 1,
    JSON.stringify(horizontalTimeline));

await goto(`${BASE}/`);
await waitForApp();
const homeTimeline = await evaluate(`(() => {
    const viewport = document.querySelector('.doc-history__timeline');
    const root = viewport && viewport.querySelector('.aeterni-timeline');
    return {
        component: !!root,
        legacy: !!document.querySelector('.doc-timeline'),
        removedLead: document.body.textContent.includes('从组件、主题到示例页面'),
        viewportHeight: viewport?.clientHeight ?? 0,
        contentHeight: viewport?.scrollHeight ?? 0,
        pageOverflow: document.documentElement.scrollHeight - window.innerHeight
    };
})()`);
check('Homepage uses Timeline and keeps the commit history inside the viewport',
    homeTimeline.component && !homeTimeline.legacy && !homeTimeline.removedLead
        && homeTimeline.viewportHeight > 0 && homeTimeline.contentHeight > homeTimeline.viewportHeight
        && homeTimeline.pageOverflow <= 1,
    JSON.stringify(homeTimeline));

await goto(`${BASE}/components/descriptions`);
await waitForApp();
const descriptions = await evaluate(`(() => {
    const root = document.querySelector('.aeterni-descriptions');
    return {
        component: !!root,
        semanticLabels: root ? root.querySelectorAll(':scope > .aeterni-descriptions__item > dt').length : 0,
        semanticValues: root ? root.querySelectorAll(':scope > .aeterni-descriptions__item > dd').length : 0,
        columns: root ? getComputedStyle(root).getPropertyValue('--aeterni-descriptions-columns').trim() : '',
        pageOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
    };
})()`);
check('Descriptions keeps native label/value semantics inside the page',
    descriptions.component && descriptions.semanticLabels === 5
        && descriptions.semanticValues === 5
        && descriptions.columns === '3'
        && descriptions.pageOverflow <= 1,
    JSON.stringify(descriptions));

for (const columns of [1, 2]) {
    await evaluate(`(() => {
        const trigger = document.querySelector('[role="combobox"][id="descriptions-columns"]');
        trigger?.click();
    })()`);
    await sleep(50);
    await evaluate(`(() => {
        const option = [...document.querySelectorAll('[role="option"]')]
            .find(element => element.textContent.trim() === '${columns}');
        option?.click();
    })()`);
    await sleep(100);
    const narrowedDescriptions = await evaluate(`(() => {
        const root = document.querySelector('.aeterni-descriptions');
        const spans = [...document.querySelectorAll('.aeterni-descriptions__item')]
            .map(item => Number.parseInt(item.style.gridColumn.match(/\\d+/)?.[0] ?? '0', 10));
        return {
            columns: root ? getComputedStyle(root).getPropertyValue('--aeterni-descriptions-columns').trim() : '',
            maxSpan: Math.max(...spans),
            pageOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
            error: document.body.textContent.includes('must be between')
        };
    })()`);
    check(`Descriptions preview remains valid with Columns=${columns}`,
        narrowedDescriptions.columns === String(columns)
            && narrowedDescriptions.maxSpan <= columns
            && narrowedDescriptions.pageOverflow <= 1
            && !narrowedDescriptions.error,
        JSON.stringify(narrowedDescriptions));
}

await goto(`${BASE}/components/stepper`);
await waitForApp();
const horizontalStepper = await evaluate(`(() => {
    const button = document.querySelector('.aeterni-stepper__button:not(.is-templated)');
    const marker = button.querySelector('.aeterni-stepper__marker');
    const icon = marker.querySelector('.aeterni-icon');
    const m = marker.getBoundingClientRect();
    const i = icon.getBoundingClientRect();
    return {
        display: getComputedStyle(button).display,
        x: Math.abs((i.left + i.width / 2) - (m.left + m.width / 2)),
        y: Math.abs((i.top + i.height / 2) - (m.top + m.height / 2))
    };
})()`);
check('Stepper default content follows the Timeline marker grid',
    horizontalStepper.display === 'grid' && horizontalStepper.x <= 0.5 && horizontalStepper.y <= 0.5,
    JSON.stringify(horizontalStepper));

await evaluate(`(() => {
    const label = [...document.querySelectorAll('label')].find(node => node.textContent.includes('纵向'));
    label.querySelector('input[type=checkbox]').click();
})()`);
await sleep(100);
const verticalStepper = await evaluate(`(() => {
    const root = document.querySelector('.aeterni-stepper');
    const button = root.querySelector('.aeterni-stepper__button:not(.is-templated)');
    const marker = button.querySelector('.aeterni-stepper__marker').getBoundingClientRect();
    const copy = button.querySelector('.aeterni-stepper__copy').getBoundingClientRect();
    return {
        vertical: root.classList.contains('aeterni-stepper--vertical'),
        display: getComputedStyle(button).display,
        markerBeforeCopy: marker.right <= copy.left + 0.5,
        aligned: Math.abs(marker.top - button.getBoundingClientRect().top) <= 0.5
    };
})()`);
check('Stepper vertical mode uses the same marker/content columns as Timeline',
    verticalStepper.vertical && verticalStepper.display === 'grid'
        && verticalStepper.markerBeforeCopy && verticalStepper.aligned,
    JSON.stringify(verticalStepper));

await evaluate(`(() => {
    const label = [...document.querySelectorAll('label')].find(node => node.textContent.includes('自定义模板'));
    label.querySelector('input[type=checkbox]').click();
})()`);
await sleep(100);
const verticalTemplateStepper = await evaluate(`(() => {
    const item = document.querySelector('.aeterni-stepper__item');
    const button = item.querySelector('.aeterni-stepper__button.is-templated');
    const template = button.querySelector('.step-template--vertical');
    const marker = template?.querySelector('.step-template__marker');
    const icon = marker?.querySelector('.aeterni-icon');
    const label = template?.querySelector('.step-template__label');
    if (!template || !marker || !icon || !label) return { error: 'vertical template not rendered' };
    const itemRect = item.getBoundingClientRect();
    const markerRect = marker.getBoundingClientRect();
    const iconRect = icon.getBoundingClientRect();
    const labelRect = label.getBoundingClientRect();
    const connector = getComputedStyle(item, '::after');
    const connectorAxis = itemRect.left + parseFloat(connector.left) + parseFloat(connector.width) / 2;
    return {
        display: getComputedStyle(template).display,
        axisDelta: Math.abs((markerRect.left + markerRect.width / 2) - connectorAxis),
        iconX: Math.abs((iconRect.left + iconRect.width / 2) - (markerRect.left + markerRect.width / 2)),
        iconY: Math.abs((iconRect.top + iconRect.height / 2) - (markerRect.top + markerRect.height / 2)),
        markerBeforeLabel: markerRect.right <= labelRect.left + 0.5,
        templateWidth: template.getBoundingClientRect().width,
        buttonWidth: button.getBoundingClientRect().width
    };
})()`);
check('Stepper vertical custom template keeps its icon on the marker axis',
    !verticalTemplateStepper.error && verticalTemplateStepper.display === 'grid'
        && verticalTemplateStepper.axisDelta <= 0.5
        && verticalTemplateStepper.iconX <= 0.5 && verticalTemplateStepper.iconY <= 0.5
        && verticalTemplateStepper.markerBeforeLabel
        && Math.abs(verticalTemplateStepper.templateWidth - verticalTemplateStepper.buttonWidth) <= 0.5,
    verticalTemplateStepper.error ?? JSON.stringify(verticalTemplateStepper));

await goto(`${BASE}/components/virtual-list`);
await waitForApp();
await evaluate(`(() => {
    const viewport = document.querySelector('.aeterni-virtual-list__viewport');
    const items = [...document.querySelectorAll('.aeterni-virtual-list__item')];
    window.__virtualListScrollBeforeClick = viewport.scrollTop;
    window.__virtualListClickedPosition = items[4].getAttribute('aria-posinset');
    items[4].click();
})()`);
await sleep(100);
const virtualListClick = await evaluate(`(() => {
    const viewport = document.querySelector('.aeterni-virtual-list__viewport');
    const selected = document.querySelector('.aeterni-virtual-list__item.is-selected');
    return {
        before: window.__virtualListScrollBeforeClick,
        after: viewport.scrollTop,
        clickedPosition: window.__virtualListClickedPosition,
        selectedPosition: selected?.getAttribute('aria-posinset') ?? null
    };
})()`);
check('VirtualList pointer selection keeps an already-visible row in place',
    virtualListClick.selectedPosition === virtualListClick.clickedPosition
        && Math.abs(virtualListClick.after - virtualListClick.before) <= 0.5,
    JSON.stringify(virtualListClick));

await evaluate(`(() => {
    const selected = document.querySelector('.aeterni-virtual-list__item.is-selected');
    const position = Number(selected.getAttribute('aria-posinset'));
    const next = document.querySelector('.aeterni-virtual-list__item[aria-posinset="' + (position + 1) + '"]');
    next.scrollIntoView({ block: 'center', inline: 'center' });
})()`);
await sleep(800);
const hoverPoint = await evaluate(`(() => {
    const selected = document.querySelector('.aeterni-virtual-list__item.is-selected');
    const position = Number(selected.getAttribute('aria-posinset'));
    const next = document.querySelector('.aeterni-virtual-list__item[aria-posinset="' + (position + 1) + '"]');
    const rect = next.getBoundingClientRect();
    window.__virtualListHoverPoint = { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
    return window.__virtualListHoverPoint;
})()`);
await mouse('mouseMoved', hoverPoint.x, hoverPoint.y, { button: 'none', buttons: 0, pointerType: 'mouse' });
await sleep(50);
const virtualListItem = await evaluate(`(() => {
    const item = document.querySelector('.aeterni-virtual-list__item');
    if (!item) return { error: 'virtual item not rendered' };
    const style = getComputedStyle(item);
    return {
        childCount: item.children.length,
        gap: style.columnGap,
        paddingBlock: style.paddingBlockStart,
        paddingInline: style.paddingInlineStart,
        radius: style.borderRadius,
        textAlign: style.textAlign
    };
})()`);
const virtualListStateGap = await evaluate(`(() => {
    const selected = document.querySelector('.aeterni-virtual-list__item.is-selected');
    const position = Number(selected.getAttribute('aria-posinset'));
    const hovered = document.querySelector('.aeterni-virtual-list__item[aria-posinset="' + (position + 1) + '"]');
    const selectedRect = selected.getBoundingClientRect();
    const hoveredRect = hovered.getBoundingClientRect();
    const selectedSurface = getComputedStyle(selected, '::before');
    const hoveredSurface = getComputedStyle(hovered, '::before');
    return {
        selectedBackground: selectedSurface.backgroundColor,
        hoveredBackground: hoveredSurface.backgroundColor,
        hoveredMatches: hovered.matches(':hover'),
        elementAtPoint: document.elementFromPoint(window.__virtualListHoverPoint.x, window.__virtualListHoverPoint.y)?.className ?? null,
        itemBoxGap: hoveredRect.top - selectedRect.bottom,
        visualGap: (hoveredRect.top + parseFloat(hoveredSurface.top))
            - (selectedRect.bottom - parseFloat(selectedSurface.bottom))
    };
})()`);

await goto(`${BASE}/components/list`);
await waitForApp();
const listItem = await evaluate(`(() => {
    const item = document.querySelector('.aeterni-list-item');
    const list = item?.closest('.aeterni-list');
    if (!item) return { error: 'list item not rendered' };
    const style = getComputedStyle(item);
    return {
        gap: style.columnGap,
        paddingBlock: style.paddingBlockStart,
        paddingInline: style.paddingInlineStart,
        radius: style.borderRadius,
        textAlign: style.textAlign,
        rowGap: getComputedStyle(list).rowGap
    };
})()`);
check('VirtualList item spacing follows List item spacing',
    !virtualListItem.error && !listItem.error && virtualListItem.childCount === 2
        && virtualListItem.gap === listItem.gap
        && virtualListItem.paddingBlock === listItem.paddingBlock
        && virtualListItem.paddingInline === listItem.paddingInline
        && virtualListItem.radius === listItem.radius
        && virtualListItem.textAlign === listItem.textAlign,
    JSON.stringify({ virtualListItem, listItem }));
check('VirtualList selected and hovered surfaces retain the List row gap',
    !virtualListStateGap.error
        && virtualListStateGap.selectedBackground !== 'rgba(0, 0, 0, 0)'
        && virtualListStateGap.hoveredBackground !== 'rgba(0, 0, 0, 0)'
        && Math.abs(virtualListStateGap.itemBoxGap) <= 0.5
        && Math.abs(virtualListStateGap.visualGap - parseFloat(listItem.rowGap)) <= 0.5,
    JSON.stringify({ virtualListStateGap, listRowGap: listItem.rowGap }));

// ---------------------------------------------------------------- REV-140
// The holo finish is split by blend role: the rainbow has to replace hue and
// saturation (`color`) while the white reflection has to lighten (`screen`). Both
// have to stay visible on light and dark artwork, which is what a real screenshot
// can answer and computed styles cannot.
await goto(`${BASE}/components/flash-card`);
await waitForApp();

const finish = await evaluate(`(() => {
    const card = document.querySelector('.aeterni-flash-card');
    if (!card) return { error: 'flash card not rendered' };
    const sheen = card.querySelector('.aeterni-flash-card__sheen');
    const light = card.querySelector('.aeterni-flash-card__sheen-light');
    if (!sheen || !light) return { error: 'finish layers missing' };
    const media = card.querySelector('.aeterni-flash-card__media').getBoundingClientRect();
    const sheenRect = sheen.getBoundingClientRect();
    return {
        sheenBlend: getComputedStyle(sheen).mixBlendMode,
        lightBlend: getComputedStyle(light).mixBlendMode,
        lightBackground: getComputedStyle(light).backgroundImage !== 'none',
        covers: Math.abs(sheenRect.width - media.width) <= 1 && Math.abs(sheenRect.height - media.height) <= 1
    };
})()`);
check('REV-140 holo splits rainbow and white light into separate blend layers',
    !finish.error && finish.sheenBlend === 'color' && finish.lightBlend === 'screen'
    && finish.lightBackground && finish.covers,
    JSON.stringify(finish));

// Screenshot the viewport, decode it *in the page* (the browser is the only PNG
// decoder available to a dependency-free harness) and average the sampled region.
async function sampleAverage(rect) {
    const shot = await send('Page.captureScreenshot', { format: 'png' });
    return evaluate(`(async () => {
        const bytes = Uint8Array.from(atob('${shot.data}'), c => c.charCodeAt(0));
        const bitmap = await createImageBitmap(new Blob([bytes], { type: 'image/png' }));
        const dpr = window.devicePixelRatio || 1;
        const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
        const ctx = canvas.getContext('2d');
        ctx.drawImage(bitmap, 0, 0);
        const data = ctx.getImageData(
            Math.round(${rect.x} * dpr), Math.round(${rect.y} * dpr),
            Math.max(1, Math.round(${rect.w} * dpr)), Math.max(1, Math.round(${rect.h} * dpr))).data;
        const sum = [0, 0, 0];
        for (let i = 0; i < data.length; i += 4) {
            sum[0] += data[i]; sum[1] += data[i + 1]; sum[2] += data[i + 2];
        }
        const count = data.length / 4;
        return sum.map(value => value / count);
    })()`);
}

// Replaces the artwork with a flat fill so light and dark backdrops are exact,
// then measures how much the finish moves the pixels. The one-shot glint is
// suppressed in both samples so only the finish is compared.
async function measureFinishDelta(fill) {
    const rect = await evaluate(`(async () => {
        const card = document.querySelector('.aeterni-flash-card');
        card.scrollIntoView({ block: 'center' });
        const img = card.querySelector('.aeterni-flash-card__media img');
        img.src = 'data:image/svg+xml,' + encodeURIComponent(
            '<svg xmlns="http://www.w3.org/2000/svg" width="8" height="8"><rect width="8" height="8" fill="${fill}"/></svg>');
        await img.decode();
        card.classList.add('is-tilting');
        card.querySelector('.aeterni-flash-card__glint').style.opacity = '0';
        const media = card.querySelector('.aeterni-flash-card__media').getBoundingClientRect();
        const inset = 14;
        return { x: media.x + inset, y: media.y + inset, w: media.width - inset * 2, h: media.height - inset * 2 };
    })()`);

    const setFinishOpacity = opacity => evaluate(`(() => {
        const card = document.querySelector('.aeterni-flash-card');
        card.querySelector('.aeterni-flash-card__sheen').style.opacity = '${opacity}';
        card.querySelector('.aeterni-flash-card__sheen-light').style.opacity = '${opacity}';
    })()`);

    await setFinishOpacity('0');
    const without = await sampleAverage(rect);
    await setFinishOpacity('');
    const withFinish = await sampleAverage(rect);
    const delta = withFinish.reduce((sum, value, index) => sum + Math.abs(value - without[index]), 0) / 3;
    return { delta };
}

const lightArt = await measureFinishDelta('#DCDCDC');
const darkArt = await measureFinishDelta('#1F1F1F');
check('REV-140 finish stays visible on light and dark artwork',
    Number.isFinite(lightArt.delta) && Number.isFinite(darkArt.delta)
    && lightArt.delta >= 6 && darkArt.delta >= 6,
    JSON.stringify({ lightDelta: lightArt.delta.toFixed(1), darkDelta: darkArt.delta.toFixed(1) }));

// ---------------------------------------------------------------- REV-146
// Pointer feedback the cascade cannot be trusted to keep: a selected ComboBox
// option has to step its fill, and the connected field groups have to light their
// shared border like standalone Input does.
await goto(`${BASE}/components/combobox`);
await waitForApp();

// Coordinates come from the viewport, so every probe scrolls its target into view
// first — otherwise the pointer lands outside the page and no :hover state exists.
const pointOf = selector => evaluate(`(() => {
    const element = document.querySelector('${selector}');
    if (!element) return null;
    element.scrollIntoView({ block: 'center', inline: 'center' });
    const rect = element.getBoundingClientRect();
    return { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 };
})()`);

const clickAt = async point => {
    await mouse('mousePressed', point.x, point.y, { button: 'left', clickCount: 1 });
    await mouse('mouseReleased', point.x, point.y, { button: 'left', clickCount: 1 });
    await sleep(150);
};

const comboTrigger = await pointOf('.aeterni-combobox__trigger');
if (comboTrigger) {
    await clickAt(comboTrigger);

    // Select the first option so there is a selected row to hover, then reopen.
    const optionPoint = await pointOf('.aeterni-combobox__option');
    if (optionPoint) {
        await clickAt(optionPoint);
        const reopened = await pointOf('.aeterni-combobox__trigger');
        await clickAt(reopened ?? comboTrigger);

        const selectedPoint = await pointOf('.aeterni-combobox__option.is-selected');
        if (selectedPoint) {
            const before = await evaluate(`getComputedStyle(document.querySelector('.aeterni-combobox__option.is-selected')).backgroundColor`);
            await mouse('mouseMoved', selectedPoint.x, selectedPoint.y);
            await sleep(120);
            const hovered = await evaluate(`(() => {
                const option = document.querySelector('.aeterni-combobox__option.is-selected');
                return { background: getComputedStyle(option).backgroundColor, matches: option.matches(':hover') };
            })()`);
            check('REV-146 a selected ComboBox option steps its fill on hover',
                hovered.matches && hovered.background !== before,
                JSON.stringify({ before, ...hovered }));
        } else {
            check('REV-146 a selected ComboBox option steps its fill on hover', false, 'no selected option to hover');
        }
    } else {
        check('REV-146 a selected ComboBox option steps its fill on hover', false, 'no option to select');
    }
} else {
    check('REV-146 a selected ComboBox option steps its fill on hover', false, 'no combobox trigger');
}

await goto(`${BASE}/components/search`);
await waitForApp();
const searchPoint = await pointOf('.aeterni-search__input-wrap');
if (searchPoint) {
    const before = await evaluate(`getComputedStyle(document.querySelector('.aeterni-search__input-wrap')).borderTopColor`);
    await mouse('mouseMoved', searchPoint.x, searchPoint.y);
    await sleep(120);
    const hovered = await evaluate(`(() => {
        const wrapper = document.querySelector('.aeterni-search__input-wrap');
        return {
            border: getComputedStyle(wrapper).borderTopColor,
            matches: wrapper.matches(':hover'),
            state: wrapper.closest('.aeterni-search').className
        };
    })()`);
    check('REV-146 the connected Search group lights its border on hover',
        hovered.matches && hovered.border !== before,
        JSON.stringify({ before, ...hovered }));
} else {
    check('REV-146 the connected Search group lights its border on hover', false, 'no search wrapper');
}

// ---------------------------------------------------------------- REV-148
// Retiring an entry has to wait for the real exit animation (300ms token) instead
// of the retired 180/220ms delays, and reduced motion has to collapse it.
await goto(`${BASE}/components/feedback`);
await waitForApp();

const toastExit = await evaluate(`(async () => {
    const trigger = [...document.querySelectorAll('button')].find(button => /^Toast$/.test(button.textContent.trim()));
    if (!trigger) return { error: 'toast trigger missing' };
    trigger.click();

    const deadline = performance.now() + 5000;
    let toast = null;
    while (performance.now() < deadline && !toast) {
        await new Promise(resolve => setTimeout(resolve, 40));
        toast = document.querySelector('.aeterni-dialog-provider__toast');
    }
    if (!toast) return { error: 'toast never appeared' };

    const close = toast.querySelector('button');
    if (!close) return { error: 'toast close button missing' };

    const start = performance.now();
    close.click();
    while (performance.now() - start < 3000 && document.body.contains(toast)) {
        await new Promise(resolve => setTimeout(resolve, 8));
    }
    return { removedAfter: Math.round(performance.now() - start), stillThere: document.body.contains(toast) };
})()`);
check('REV-148 the toast plays its full exit animation before it is retired',
    !toastExit.error && !toastExit.stillThere && toastExit.removedAfter >= 250 && toastExit.removedAfter <= 900,
    JSON.stringify(toastExit));

await setReducedMotion(true);
const reducedExit = await evaluate(`(async () => {
    const trigger = [...document.querySelectorAll('button')].find(button => /^Toast$/.test(button.textContent.trim()));
    if (!trigger) return { error: 'toast trigger missing' };
    trigger.click();

    const deadline = performance.now() + 5000;
    let toast = null;
    while (performance.now() < deadline && !toast) {
        await new Promise(resolve => setTimeout(resolve, 40));
        toast = document.querySelector('.aeterni-dialog-provider__toast');
    }
    if (!toast) return { error: 'toast never appeared' };

    const close = toast.querySelector('button');
    if (!close) return { error: 'toast close button missing' };

    const start = performance.now();
    close.click();
    while (performance.now() - start < 2000 && document.body.contains(toast)) {
        await new Promise(resolve => setTimeout(resolve, 8));
    }
    return { removedAfter: Math.round(performance.now() - start), stillThere: document.body.contains(toast) };
})()`);
await setReducedMotion(false);
check('REV-148 reduced motion collapses the exit wait instead of blocking',
    !reducedExit.error && !reducedExit.stillThere && reducedExit.removedAfter <= 250,
    JSON.stringify(reducedExit));

console.log(`\n${results.filter(r => r.ok).length}/${results.length} checks passed`);
const failed = results.some(r => !r.ok);
console.log(failed ? 'RESULT: FAIL' : 'RESULT: PASS');
close();
// Without this the harness printed FAIL and still exited 0, so `run.sh` (and the
// CI browser job) reported success while checks were broken.
if (failed) {
    process.exitCode = 1;
}
