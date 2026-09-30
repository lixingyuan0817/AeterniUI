// Browser verification for the three changes the ninth review round could not
// confirm without a real engine:
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
    const deadline = Date.now() + 5000;
    while (Date.now() < deadline) {
        if (await evaluate('window.__t1 !== null')) break;
        await sleep(25);
    }
    const observed = await evaluate(`JSON.stringify({ seen: window.__seen, t0: window.__t0, t1: window.__t1 })`);
    const { seen, t0, t1 } = JSON.parse(observed);
    return { appliedImmediately, sawClass: seen, removedAfter: t0 !== null && t1 !== null ? Math.round(t1 - t0) : null };
}

const normal = await measureTransition(2);
check('REV-121 the transition class is applied on a real theme switch', normal.appliedImmediately && normal.sawClass,
    `applied=${normal.appliedImmediately} observed=${normal.sawClass}`);
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

console.log(`\n${results.filter(r => r.ok).length}/${results.length} checks passed`);
console.log(results.some(r => !r.ok) ? 'RESULT: FAIL' : 'RESULT: PASS');
close();
