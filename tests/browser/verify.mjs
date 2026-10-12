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
// 上界放宽到 950ms：窗口要证明的是"等待来自 Token 的约 620ms"，而不是"精确等于 620ms"。
// 在负载较高的 CI runner 上，定时器回调会晚 100～150ms 触发（连续两次实测 770ms / 755ms），
// 卡在 750ms 只会得到一个与功能无关的红灯；下界 550ms 仍然拦住"立刻移除"，reduced-motion
// 那条断言（≤200ms）则继续守住"真的按偏好收敛"。
check('REV-121 it is removed once the token-derived duration elapses (~620ms)',
    normal.removedAfter !== null && normal.removedAfter >= 550 && normal.removedAfter <= 950,
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
// 顺序焦点起点必须先归零：Chrome 会记住最后一次点击/拖动的元素，之后的 Tab 从它**后面**
// 继续。P7 先拖了第一个滑块，于是 Tab 从它后面开始，断言落在了第二个实例上（日志里的
// focus.index=1 就是这么来的）。把 body 变成可聚焦元素并聚焦它，起点就回到文档开头。
async function tabUntil(selector, maxPresses = 40, anchor = null) {
    // 有锚点时从锚点起 Tab：Chrome 的顺序焦点起点会被之前的点击/拖动带走（body.focus() 不会
    // 把它归零），P7 先拖了第一个滑块，Tab 于是从它后面开始，断言落到了第二个实例上。
    // 从页面标题（tabindex="-1"，不入 Tab 序）起走，第一步就落在页面内容的第一个可聚焦元素上。
    await evaluate(anchor
        ? `(() => { const node = document.querySelector('${anchor}'); if (node) node.focus(); })()`
        : `(() => {
        if (document.body.tabIndex !== -1) document.body.tabIndex = -1;
        document.activeElement && document.activeElement.blur();
        document.body.focus();
    })()`);
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
const sliderReached = await tabUntil('[role="slider"]', 6, '#slider h1');
// 诊断信息随断言一起打印：页面变长后"第几个滑块被聚焦"决定了下面两条断言的对象，
// 猜不出来时就让日志说话。
const sliderFocus = await evaluate(`(() => {
    const sliders = [...document.querySelectorAll('[role="slider"]')];
    return {
        count: sliders.length,
        index: sliders.indexOf(document.activeElement),
        active: document.activeElement?.getAttribute('aria-label') ?? null,
        tabindex: document.activeElement?.getAttribute('tabindex') ?? null,
        each: sliders.map((node, i) => {
            const style = getComputedStyle(node);
            const rect = node.getBoundingClientRect();
            return [
                i,
                node.getAttribute('aria-label'),
                'tab=' + (node.getAttribute('tabindex') ?? '-'),
                'display=' + style.display,
                'visibility=' + style.visibility,
                'w=' + Math.round(rect.width),
                'h=' + Math.round(rect.height),
                'inert=' + node.closest('[inert]')?.tagName,
                'hidden-ancestor=' + (node.closest('[hidden]')?.tagName ?? '-')
            ].join('|');
        })
    };
})()`);
const thumbRing = await evaluate(`(() => {
    const style = getComputedStyle(document.querySelector('.aeterni-slider__thumb'));
    return { width: style.outlineWidth, style: style.outlineStyle, color: style.outlineColor };
})()`);
check('P7 keyboard focus paints the ring on the thumb',
    sliderReached && sliderFocus.index === 0 && parseFloat(thumbRing.width) > 0
    && thumbRing.style !== 'none' && !/rgba?\(0, 0, 0, 0\)/.test(thumbRing.color),
    `reached=${sliderReached} width=${thumbRing.width} style=${thumbRing.style} color=${thumbRing.color} focus=${JSON.stringify(sliderFocus)}`);

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
    sliderFocus.index === 0 && sliderAfterKeys.value === '65.00' && sliderAfterKeys.scrollable
    && sliderAfterKeys.scrollY === scrollTopBefore,
    `value 75.00 -> ${sliderAfterKeys.value}, scrollY ${scrollTopBefore} -> ${sliderAfterKeys.scrollY} (scrollable=${sliderAfterKeys.scrollable}) focus=${JSON.stringify(sliderFocus)}`);

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
    // 必须锁定被那个复选框控制的实例：卡片化之后一页有三段时间轴，first-match 拿到的是
    // "基础用法"里那一段，它不该跟着变（检查项因此误报 horizontal=false）。
    const root = document.querySelector('#timeline-controls .aeterni-timeline');
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
// A style change is only visible once the browser painted it: capturing straight
// after toggling the finish produced the *previous* frame (REV-140 measured 0.0).
async function settleFrame() {
    await evaluate('new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))');
    await sleep(80);
}

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
        card.scrollIntoView({ block: 'center', behavior: 'instant' });
        const img = card.querySelector('.aeterni-flash-card__media img');
        img.src = 'data:image/svg+xml,' + encodeURIComponent(
            '<svg xmlns="http://www.w3.org/2000/svg" width="8" height="8"><rect width="8" height="8" fill="${fill}"/></svg>');
        await img.decode();
        card.classList.add('is-tilting');
        // Pin the reflection at the centre: the pointer drives the sheen phase, and
        // an earlier check may have parked it outside the card, which made the
        // measured delta depend on test order instead of on the finish (REV-140).
        card.style.setProperty('--aeterni-flash-card-sheen-x', '50%');
        card.style.setProperty('--aeterni-flash-card-sheen-y', '50%');
        card.querySelector('.aeterni-flash-card__glint').style.opacity = '0';
        const media = card.querySelector('.aeterni-flash-card__media').getBoundingClientRect();
        const size = Math.min(24, media.width / 3, media.height / 3);
        return {
            x: media.x + media.width / 2 - size / 2,
            y: media.y + media.height / 2 - size / 2,
            w: size,
            h: size
        };
    })()`);

    const setFinishOpacity = opacity => evaluate(`(() => {
        const card = document.querySelector('.aeterni-flash-card');
        card.querySelector('.aeterni-flash-card__sheen').style.opacity = '${opacity}';
        card.querySelector('.aeterni-flash-card__sheen-light').style.opacity = '${opacity}';
    })()`);

    await setFinishOpacity('0');
    await settleFrame();
    const without = await sampleAverage(rect);
    await setFinishOpacity('');
    await settleFrame();
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
// The connected field group owns the visible border (the inner Input's border is
// flattened by the group rule), so its pointer feedback is a cascade question a
// computed style cannot answer. The ComboBox selected-option step is asserted at
// the stylesheet level in the contract suite; here the pointer path is exercised on
// the Search group, which uses the same mechanism.
await goto(`${BASE}/components/search`);
await waitForApp();

const searchPoint = await evaluate(`(() => {
    const wrapper = document.querySelector('.aeterni-search__input-wrap');
    if (!wrapper) return null;
    // The sample sets a smooth scroll behaviour: an animated scroll would leave the
    // rect stale and the pointer on the page background instead of the control.
    wrapper.scrollIntoView({ block: 'center', behavior: 'instant' });
    const rect = wrapper.getBoundingClientRect();
    return { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 };
})()`);
if (searchPoint) {
    const before = await evaluate(`getComputedStyle(document.querySelector('.aeterni-search__input-wrap')).borderTopColor`);
    await mouse('mouseMoved', searchPoint.x, searchPoint.y, { button: 'none', buttons: 0, pointerType: 'mouse' });
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

// ---------------------------------------------------------------- Upload (10.32.0)
// A file drop never lands in the input element by itself: the module copies the
// event's FileList onto the picker and dispatches `change` so Blazor reads it.
// Whether that bridge actually works is a real-engine fact, so it is asserted
// here rather than only in the module's DOM-double tests.
await goto(`${BASE}/components/upload`);
await waitForApp();

// 断言限定在「宿主驱动传输进度」那张卡片上：卡片化之后一页有四个 Upload 实例，
// 全局选择器会命中第一张（基础用法，没有传输逻辑），检查就会对着一个不该有进度的
// 实例断言进度。
//
// 拖放监听由组件的 JS 模块在渲染后 attach。`waitForApp` 只证明标记渲染完成，卡片化之后
// 一张页面上有四个 Upload 实例，第四个实例的模块还没接上监听时，派发 dragenter 会毫无
// 反应——所以先用探针确认区域已经会画拖放态，再断言（CI 抓到的正是这次的时序）。
async function dropZoneReady(selector) {
    const deadline = Date.now() + 10000;
    while (Date.now() < deadline) {
        const ready = await evaluate(`(() => {
            const zone = document.querySelector('${selector}');
            if (!zone) return false;
            const transfer = new DataTransfer();
            transfer.items.add(new File(['aeterni'], 'probe.txt', { type: 'text/plain' }));
            zone.dispatchEvent(new DragEvent('dragenter', { bubbles: true, dataTransfer: transfer }));
            const painted = zone.classList.contains('is-dragover');
            zone.classList.remove('is-dragover');
            return painted;
        })()`);
        if (ready) return true;
        await sleep(150);
    }
    return false;
}

// `drop` hands over files the page built, exactly like a real transfer: the
// FileList a drop carries is the one form a script is allowed to assign.
const dragEnter = () => evaluate(`(() => {
    const zone = document.querySelector('#upload-host-progress .aeterni-upload__drop');
    const transfer = new DataTransfer();
    transfer.items.add(new File(['aeterni'], 'dragged.txt', { type: 'text/plain' }));
    zone.dispatchEvent(new DragEvent('dragenter', { bubbles: true, dataTransfer: transfer }));
    return { highlighted: zone.classList.contains('is-dragover'), files: document.querySelector('#upload-host-progress input[type=file]').files.length };
})()`);

const dropFile = name => evaluate(`(() => {
    const zone = document.querySelector('#upload-host-progress .aeterni-upload__drop');
    const input = document.querySelector('#upload-host-progress input[type=file]');
    const transfer = new DataTransfer();
    transfer.items.add(new File(['aeterni'], '${name}', { type: 'text/plain' }));
    zone.dispatchEvent(new DragEvent('dragover', { bubbles: true, dataTransfer: transfer }));
    zone.dispatchEvent(new DragEvent('drop', { bubbles: true, dataTransfer: transfer }));
    return { highlighted: zone.classList.contains('is-dragover'), files: input.files.length };
})()`);

async function waitForRows(count) {
    const deadline = Date.now() + 10000;
    let rows = 0;
    while (Date.now() < deadline) {
        rows = await evaluate(`document.querySelectorAll('#upload-host-progress .aeterni-upload__item').length`);
        if (rows >= count) break;
        await sleep(100);
    }
    return rows;
}

const zoneReady = await dropZoneReady('#upload-host-progress .aeterni-upload__drop');
const drag = await dragEnter();
check('Upload paints its drop state while a file is dragged over the zone',
    zoneReady && drag.highlighted && drag.files === 0,
    `ready=${zoneReady} highlighted=${drag.highlighted} files=${drag.files}`);

const dropped = await dropFile('dragged.txt');
check('Upload clears the drop state on drop', !dropped.highlighted, `highlighted=${dropped.highlighted}`);

const firstRow = await waitForRows(1);
check('Upload reports the dropped file to Blazor through the native picker', firstRow >= 1, `${firstRow} row(s)`);
const metadata = await evaluate(
    `/7 B/.test(document.querySelector('#upload-host-progress .aeterni-upload__item')?.innerText ?? '')`);
check('Upload reads the dropped file metadata', metadata, 'the dropped File size must reach C#');

// The sample fabricates the transport on the host side; the row must follow it.
const deadline = Date.now() + 10000;
let uploaded = false;
while (Date.now() < deadline) {
    uploaded = await evaluate(`/Uploaded/.test(document.querySelector('#upload-host-progress .aeterni-upload__item')?.innerText ?? '')`);
    if (uploaded) break;
    await sleep(100);
}
check('Upload renders the host-reported transfer status', uploaded, 'the row never reached the success state');

await dropFile('second.txt');
const twoRows = await waitForRows(2);
// The picker's `files` is deliberately not asserted: whether the framework has
// already read them is a timing detail, and asserting it would pin the harness to
// a race instead of to the behaviour. What must hold is that both files are listed.
const names = await evaluate(
    `[...document.querySelectorAll('#upload-host-progress .aeterni-upload__item-name')].map(node => node.textContent.trim()).join(',')`);
check('Upload appends a second drop instead of replacing the list',
    twoRows >= 2 && names.includes('dragged.txt') && names.includes('second.txt'),
    `rows=${twoRows} names=${names}`);

// ---------------------------------------------------------------- 文档壳
// 标准组件文档页的骨架：侧栏检索、每页的源码入口、目录顺序里的前后邻居，以及可复制
// 的示例代码。这些结论都要真实 DOM 才成立——MenuItem 的 href 是运行时拼的，复制按钮
// 走的是应用级事件委托，静态检查看不到任何一条。
await goto(`${BASE}/components/upload`);
await waitForApp();

const shell = await evaluate(`(() => {
    const edit = document.querySelector('.component-page__edit');
    const prev = document.querySelector('.showcase-doc-nav__link[rel="prev"]');
    const next = document.querySelector('.showcase-doc-nav__link[rel="next"]');
    // 代码默认折叠：先点开第一张卡片的"代码" tab，再检查代码块与复制按钮。
    const codeTab = document.querySelector('.component-demo__tab[id$="-code-tab"]');
    codeTab?.click();
    const block = document.querySelector('.component-preview__code-block code');
    const copy = document.querySelector('[data-sample-copy]');
    return {
        hasSearch: !!document.querySelector('#showcase-search'),
        // 条目计数已从侧栏移除：它是给开发者的信息，读者只需要筛选结果本身。
        hasCount: !!document.querySelector('.showcase-search__status'),
        editHref: edit?.getAttribute('href') ?? null,
        editTarget: edit?.getAttribute('target') ?? null,
        prevHref: prev?.getAttribute('href') ?? null,
        nextHref: next?.getAttribute('href') ?? null,
        codeHead: block?.textContent?.trim().slice(0, 20) ?? null,
        copyTarget: copy?.dataset.sampleCopy ?? null,
        copyBlockExists: copy ? !!document.getElementById(copy.dataset.sampleCopy) : false
    };
})()`);
check('文档壳把检索独立成块，且不再显示条目计数',
    shell.hasSearch && shell.hasCount === false, JSON.stringify(shell));
check('文档壳的源码入口指向本页文件',
    shell.editHref === 'https://github.com/lixingyuan0817/AeterniUI/blob/main/src/AeterniUI.Sample/Pages/Components/Upload.razor'
    && shell.editTarget === '_blank',
    `href=${shell.editHref}`);
check('文档壳按目录顺序给出前后邻居',
    shell.prevHref === '/components/multi-select' && shell.nextHref === '/components/combobox',
    `prev=${shell.prevHref} next=${shell.nextHref}`);
check('示例代码段渲染了可复制的片段',
    (shell.codeHead ?? '').startsWith('<Upload') && shell.copyBlockExists && (shell.copyTarget ?? '').endsWith('-code-block'),
    JSON.stringify(shell));

// 复制按钮的接线：真实点击必须走到脚本并把结果写回按钮（成功或失败都算接线正确，
// 无头浏览器里剪贴板权限本就不保证）。
await evaluate(`document.querySelector('[data-sample-copy]')?.click()`);
await sleep(200);
const copyState = await evaluate(`(() => {
    const button = document.querySelector('[data-sample-copy]');
    return { state: button?.dataset.sampleCopyState ?? null, label: button?.textContent?.trim() ?? null };
})()`);
check('复制按钮点击后回报结果而不是静默失败',
    copyState.state === 'copied' || copyState.state === 'failed',
    JSON.stringify(copyState));

// 页内目录（右栏 TOC）：只在有位置时出现，所以先把视口临时撑到 1440×900 —— 无头 Chrome
// 默认 800×600，窄屏下目录是收起的（正文可读宽度优先）。
await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
await sleep(200);

const toc = await evaluate(`(() => {
    const aside = document.querySelector('.component-page__toc');
    if (!aside) return { error: 'no toc' };
    const links = [...aside.querySelectorAll('.component-page__toc-link')];
    return {
        count: links.length,
        titles: links.map(node => node.textContent.trim()),
        deadLinks: links
            .map(node => (node.getAttribute('href') ?? '').slice(1))
            .filter(id => !document.getElementById(id))
    };
})()`);
check('右栏目录列出本页各段，且没有死链',
    !toc.error && toc.count >= 6 && toc.deadLinks.length === 0 && toc.titles[0] === '何时使用',
    JSON.stringify(toc));

// 点最后一条（说明与边界）应真正滚到那一段，并在滚动后高亮它。
await evaluate(`(() => {
    const links = [...document.querySelectorAll('.component-page__toc-link')];
    links[links.length - 1].click();
})()`);
await sleep(600);
const tocAfterJump = await evaluate(`(() => {
    const links = [...document.querySelectorAll('.component-page__toc-link')];
    const last = links[links.length - 1];
    const id = (last.getAttribute('href') ?? '').slice(1);
    const section = document.getElementById(id);
    const current = document.querySelector('.component-page__toc-link.is-current');
    return {
        scrolled: window.scrollY > 0,
        sectionVisible: section ? section.getBoundingClientRect().top < window.innerHeight : false,
        current: current ? current.getAttribute('href') : null,
        ariaCurrent: current ? current.getAttribute('aria-current') : null
    };
})()`);
check('目录链接跳到对应段落',
    tocAfterJump.scrolled && tocAfterJump.sectionVisible, JSON.stringify(tocAfterJump));
check('滚动后高亮当前段（is-current + aria-current）',
    tocAfterJump.current === '#' + 'upload-notes-title' && tocAfterJump.ariaCurrent === 'location',
    JSON.stringify(tocAfterJump));

await send('Emulation.clearDeviceMetricsOverride');
await sleep(150);

// 卡片化页面：一页多段 demo，每段自带锚点、说明与代码。先重新导航一次——折叠态说的是初始
// 状态，而上面的壳检查已经点开过一个"代码"tab。
await goto(`${BASE}/components/upload`);
await waitForApp();

//这是市面组件文档的页面模板，
// 也是批次 3 的可见成果——此前每页只有一段演示。
const cards = await evaluate(`(() => {
    const sections = [...document.querySelectorAll('.component-demo')];
    return {
        count: sections.length,
        anchors: sections.map(node => node.id),
        codeTabs: sections.filter(node => node.querySelector('.component-demo__tab[id$="-code-tab"]')).length,
        // 代码默认折叠：代码面板仍带 hidden。注意不能数 DOM 里的代码块——querySelectorAll
        // 照样会数到隐藏面板内部，那样断言永远是"折叠失败"。
        openCodePanels: [...document.querySelectorAll('[role="tabpanel"][id$="-code"]')]
            .filter(panel => !panel.hasAttribute('hidden')).length,
        when: !!document.querySelector('.component-page__when'),
        related: document.querySelectorAll('.component-page__related a').length,
        api: document.querySelectorAll('.component-preview__api-row').length,
        edit: document.querySelector('.component-page__edit')?.getAttribute('href') ?? null
    };
})()`);
check('组件页按 demo 卡片组织，每段带锚点与"代码"tab',
    cards.count >= 3 && cards.codeTabs === cards.count && cards.anchors.every(anchor => anchor.length > 0),
    JSON.stringify(cards));
check('代码默认折叠在 tab 后面', cards.openCodePanels === 0, JSON.stringify(cards));
const insets = await evaluate(`(() => {
    const card = document.querySelector('.component-preview');
    const left = node => node ? Math.round(node.getBoundingClientRect().left - card.getBoundingClientRect().left) : null;
    const container = document.querySelector('#upload');
    // 量内容盒而不是段元素：padding 在段元素内部，段的 rect 底边本来就贴着卡片底边，
    // 量段本身只会得到 0（这一版断言就写错过一次）。
    const notes = document.querySelector('.component-preview__notes');
    return {
        header: left(document.querySelector('.component-preview__header h1')),
        demo: left(document.querySelector('.component-demo h3')),
        api: left(document.querySelector('.component-preview__api h4')),
        notesBottom: notes
            ? Math.round(card.getBoundingClientRect().bottom - notes.getBoundingClientRect().bottom)
            : null,
        container: container ? Math.round(container.getBoundingClientRect().width) : null
    };
})()`);
check('卡片各段左右内边距一致（页头 / demo / API）',
    insets.header !== null && insets.header === insets.demo && insets.header === insets.api,
    JSON.stringify(insets));
check('说明与边界仍保留底部内边距',
    insets.notesBottom !== null && insets.notesBottom > 8,
    JSON.stringify(insets));

check('「在 GitHub 编辑」在卡片内部',
    (cards.edit ?? '').endsWith('/src/AeterniUI.Sample/Pages/Components/Upload.razor'),
    `href=${cards.edit}`);
check('页面带「何时使用」「相关组件」与生成式 API 表',
    cards.when && cards.related >= 3 && cards.api > 15,
    JSON.stringify(cards));

// 卡片化的覆盖面：单独抽一两页断言不够——批次 3 的目标是整组页面都变成"多段 demo +
// 每段代码"，所以按组分批抽查，任何一页退回单段形态都会在这里失败。
for (const path of ['/components/input', '/components/checkbox', '/components/radio', '/components/multi-select']) {
    await goto(`${BASE}${path}`);
    await waitForApp();
    const shape = await evaluate(`(() => {
        const sections = [...document.querySelectorAll('.component-demo')];
        return {
            cards: sections.length,
            codeTabs: sections.filter(node => node.querySelector('.component-demo__tab[id$="-code-tab"]')).length,
            uniqueAnchors: new Set(sections.map(node => node.id)).size,
            when: !!document.querySelector('.component-page__when')
        };
    })()`);
    check(`卡片化 ${path}：多段 demo、锚点唯一、每段带代码 tab、有何时使用`,
        shape.cards >= 3 && shape.codeTabs === shape.cards && shape.uniqueAnchors === shape.cards && shape.when,
        JSON.stringify(shape));
}

// 生成式 API 表：断言必须回到 upload 页再跑——上面那组跨页卡片抽查把浏览器留在了别的
// 路由上，而这里要核对的是 Upload 自己的那一行。
await goto(`${BASE}/components/upload`);
await waitForApp();

// 迁移之后页面不再手写任何参数行，这里验证渲染结果确实来自生成物——
// `MaxFileSize` 是 Upload 的公共参数，而且只由 10.32.0 引入，所以「自 10.32.0」这一列
// 同时证明了生成物与编辑源（api-since.json）都被用上了。
const apiTable = await evaluate(`(() => {
    const rows = [...document.querySelectorAll('.component-preview__api-row')]
        .map(row => row.textContent.replace(/\\s+/g, ' ').trim());
    return {
        count: rows.length,
        upload: rows.find(text => text.startsWith('MaxFileSize')) ?? '',
        base: rows.find(text => text.startsWith('Id ')) ?? rows.find(text => text.startsWith('Id')) ?? ''
    };
})()`);
check('API 表由生成物渲染，并带“自哪个版本”列',
    apiTable.count > 15 && /MaxFileSize/.test(apiTable.upload) && /自 10\.32\.0/.test(apiTable.upload),
    JSON.stringify(apiTable));
check('基类参数并入生成表', /AeterniComponent|组件基类|根元素/.test(apiTable.base), apiTable.base);

// 侧栏检索过滤：输入“日期”后菜单项应明显减少，且状态行报出匹配数。
const filtered = await evaluate(`(async () => {
    const search = document.querySelector('#showcase-search');
    const before = document.querySelectorAll('.aeterni-menu__item').length;
    search.value = '日期';
    search.dispatchEvent(new Event('input', { bubbles: true }));
    const deadline = performance.now() + 4000;
    let after = before;
    while (performance.now() < deadline) {
        await new Promise(resolve => setTimeout(resolve, 50));
        after = document.querySelectorAll('.aeterni-menu__item').length;
        if (after < before) break;
    }
    return { before, after, status: document.querySelector('.showcase-search__status')?.textContent?.trim() ?? null };
})()`);
// 不再断言计数行：条目计数已从侧栏移除，筛选结果本身就是反馈。
check('侧栏检索按查询过滤条目',
    filtered.after > 0 && filtered.after < filtered.before,
    JSON.stringify(filtered));

// ---------------------------------------------------------------- REV-159
// The sample pages are the library's runnable contract: the demos the review asked
// for are asserted here instead of being trusted to exist.
const sampleDemos = [
    { path: '/components/upload', selector: '.aeterni-upload input[type="file"]', note: 'file picker and drop zone' },
    { path: '/components/progress', selector: '#progress-value', note: 'interactive Value/Color/Size controls' },
    { path: '/components/progress', selector: '.aeterni-progress', minimum: 5, note: 'a bar per semantic colour' },
    { path: '/components/icon-button', selector: 'button.aeterni-icon-button[disabled]', note: 'disabled state' },
    { path: '/components/icon-button', selector: 'form button.aeterni-icon-button[type="submit"]', note: 'submit type inside a form' },
    { path: '/components/menu-button', selector: '.aeterni-menu-button', minimum: 4, note: 'controlled open, disabled and loading variants' },
    { path: '/components/segmented', selector: '[aria-invalid="true"]', note: 'invalid state' },
    { path: '/components/segmented', selector: '[aria-required="true"]', note: 'required state' },
    { path: '/components/tag', selector: '.aeterni-tag__icon', minimum: 2, note: 'start and end icons' },
    { path: '/components/avatar', selector: '.aeterni-avatar img', note: 'image avatar with fallback' },
    { path: '/components/input-number', selector: 'input[role="spinbutton"]', note: 'numeric field' },
    { path: '/components/input-number', selector: 'form button[type="submit"]', note: 'EditContext validation chain' },
];

let currentPage = null;
for (const demo of sampleDemos) {
    if (currentPage !== demo.path) {
        await goto(`${BASE}${demo.path}`);
        await waitForApp();
        currentPage = demo.path;
    }

    const found = await evaluate(`(() => {
        const nodes = document.querySelectorAll('${demo.selector}');
        return nodes.length;
    })()`);
    const ok = demo.minimum ? found >= demo.minimum : found > 0;
    const detail = ok
        ? `${found} match(es) for ${demo.selector}`
        : `${found} match(es) for ${demo.selector}; ${JSON.stringify(await evaluate(`(() => {
            const errorUi = document.querySelector('#blazor-error-ui');
            return {
                forms: document.querySelectorAll('form').length,
                spinbuttons: document.querySelectorAll('input[role="spinbutton"]').length,
                errorUiVisible: errorUi ? getComputedStyle(errorUi).display : null,
                head: document.body.innerText.replace(/\s+/g, ' ').slice(0, 120)
            };
        })()`))}`;
    check(`REV-159 ${demo.path} demonstrates ${demo.note}`, ok, detail);
}

// Presets only exist while the range picker is open, so the demo opens it first.
await goto(`${BASE}/components/date-picker`);
await waitForApp();
const presets = await evaluate(`(async () => {
    const trigger = document.querySelector('.aeterni-date-range-picker button, button[aria-haspopup="dialog"]');
    if (!trigger) return { error: 'no range picker trigger' };
    trigger.click();
    const deadline = performance.now() + 4000;
    while (performance.now() < deadline && !document.querySelector('.aeterni-date-range-picker__preset')) {
        await new Promise(resolve => setTimeout(resolve, 50));
    }
    return { count: document.querySelectorAll('.aeterni-date-range-picker__preset').length };
})()`);
check('REV-159 /components/date-picker demonstrates host presets',
    !presets.error && presets.count >= 3,
    JSON.stringify(presets));

console.log(`\n${results.filter(r => r.ok).length}/${results.length} checks passed`);
const failed = results.some(r => !r.ok);
console.log(failed ? 'RESULT: FAIL' : 'RESULT: PASS');
close();
// Without this the harness printed FAIL and still exited 0, so `run.sh` (and the
// CI browser job) reported success while checks were broken.
if (failed) {
    process.exitCode = 1;
}
