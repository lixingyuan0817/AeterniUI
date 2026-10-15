// Browser verification for conclusions HtmlRenderer cannot confirm without a
// real engine. The original three ninth-review checks remain the first group:
//   REV-121  theme transition timing derived from tokens, collapsing under
//            prefers-reduced-motion
//   REV-118  focus ring still painted after the alias was removed
//   REV-123  roving focus still works after ToggleGroup/Toolbar were merged
import { connect, makeHelpers } from './cdp.mjs';
import { readFileSync, readdirSync } from 'node:fs';

const BASE = process.env.AETERNI_BASE_URL ?? 'http://127.0.0.1:5099';

// 整站体检要读页面源码，因此用 import.meta.url 定位，不依赖调用者的工作目录。
// 尾斜杠不能省：`new URL(name, dir)` 会把目录的最后一段替换掉。
const SHOWCASE_PAGES = new URL('../../src/AeterniUI.Sample/Pages/Components/', import.meta.url);

const { send, once, close } = await connect();
await send('Page.enable');
await send('Runtime.enable');
const { evaluate, goto, waitForApp, key, press, setReducedMotion } = makeHelpers({ send, once });

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
// 起点锚定到本页标题、诊断也限定在本页：页头现在挂着搜索入口按钮，它在文档里排在页面内容
// 之前，而这条断言的"被重新指向的消费者"是主题页的 ComboBox——不限定的话诊断会量到页头那个
// 控件（实测 outline-style=none，因为它当时根本没有焦点）。
const reached = await tabUntil('[role="combobox"]', 40, '#theme h1');
const ring = await evaluate(`(() => {
    const trigger = document.querySelector('#theme [role="combobox"]');
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
// 首页动画在首帧之后由 sample-home.js 接管：等它挂上 homeReady 再断言，
// 顺带给入场动画留一拍，避免把"正在入场"误判成"卡住了"。
for (let attempt = 0; attempt < 40; attempt++) {
    if (await evaluate(`document.querySelector('.doc-home')?.dataset.homeReady === 'true'`)) break;
    await new Promise(resolve => setTimeout(resolve, 250));
}
await new Promise(resolve => setTimeout(resolve, 900));
const homeShowcase = await evaluate(`(() => {
    const cards = [...document.querySelectorAll('.doc-showcase__card')];
    return {
        ready: document.querySelector('.doc-home')?.dataset.homeReady === 'true',
        cards: cards.length,
        visible: cards.filter(card => Number(getComputedStyle(card).opacity) > .9).length,
        groups: document.querySelectorAll('.doc-showcase__group').length,
        timeline: !!document.querySelector('.doc-history__timeline'),
        gsap: typeof window.gsap !== 'undefined',
        horizontalOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
    };
})()`);
check('Homepage lists real components and GSAP brings them in',
    homeShowcase.ready && !homeShowcase.timeline && homeShowcase.gsap
        && homeShowcase.cards >= 12 && homeShowcase.visible === homeShowcase.cards
        && homeShowcase.groups >= 3 && homeShowcase.horizontalOverflow <= 1,
    JSON.stringify(homeShowcase));

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

// ---------------------------------------------------------------- Segmented 圆角与胶囊
// `Shape` 是 Segmented 的第二个枚举档位：默认 Rounded 换成控件圆角、不输出修饰类，Pill
// 换成整圆；两档都不得改变滑块的宽度与行程。
//
// 断言全部限定到具体实例：这一页有多组分段控件（主题页的两组也在这个应用里），
// `querySelector('.aeterni-segmented')` 会量到别的那一组——本项目已经因为 first-match
// 量错实例失败过多次。受控件控制的那组靠 aria-label 定位，另一组是同页的默认档对照。
await goto(`${BASE}/components/segmented`);
await waitForApp();

const segmentedShape = () => evaluate(`(() => {
    const byLabel = label => [...document.querySelectorAll('.aeterni-segmented')]
        .find(node => node.getAttribute('aria-label') === label) ?? null;
    const read = label => {
        const root = byLabel(label);
        if (!root) return null;
        const option = root.querySelector('.aeterni-segmented__option');
        const slider = root.querySelector('.aeterni-segmented__slider');
        const optionBox = option.getBoundingClientRect();
        const sliderBox = slider.getBoundingClientRect();
        const trackBox = root.getBoundingClientRect();
        const token = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        // 计算值给的是声明半径：9999px 这类值要到用值阶段才被裁到半高，判断圆角是否同心
        // 必须拿裁过的半径，否则 Pill 档会被读成"半径变了"。
        const used = (value, size) => Math.min(Number.parseFloat(value), size / 2);
        const trackStyle = getComputedStyle(root);
        const optionStyle = getComputedStyle(option);
        const trackRadiusUsed = used(trackStyle.borderTopLeftRadius, trackBox.height);
        const sliderRadiusUsed = used(getComputedStyle(slider).borderTopLeftRadius, sliderBox.height);
        const padding = Number.parseFloat(trackStyle.paddingLeft);
        const border = Number.parseFloat(trackStyle.borderTopWidth);
        // 圆角处的留白：沿角平分线量轨道描边内沿到滑块圆角的距离。两个圆角同心时它等于
        // 四边留白；滑块复用轨道半径时角上会更宽（改前默认档：四边 5px、角上 7.5px）。
        const cornerGap = (Math.SQRT2 * (border + padding + sliderRadiusUsed) - sliderRadiusUsed)
            - (Math.SQRT2 * trackRadiusUsed - (trackRadiusUsed - border));
        return {
            classes: root.className,
            radius: Number.parseFloat(trackStyle.borderTopLeftRadius),
            optionRadius: Number.parseFloat(optionStyle.borderTopLeftRadius),
            sliderRadius: Number.parseFloat(getComputedStyle(slider).borderTopLeftRadius),
            trackRadiusUsed,
            sliderRadiusUsed,
            optionRadiusUsed: used(optionStyle.borderTopLeftRadius, optionBox.height),
            sliderHeight: sliderBox.height,
            padding,
            border,
            cornerGap: Number(cornerGap.toFixed(2)),
            // 滑块几何：相对同一列（当前选中项在 index 0）的偏移、自身宽度与轨道宽度。
            offset: sliderBox.left - optionBox.left,
            sliderWidth: sliderBox.width,
            optionWidth: optionBox.width,
            trackWidth: trackBox.width,
            // 轨道高度 / 选项高度 / 滑块四边留白：padding 同时喂给轨道、滑块与滑块的行程，
            // 一旦调过头，选项就会跌破 24px 热区并把默认档顶到 38px。
            trackHeight: trackBox.height,
            optionHeight: optionBox.height,
            pillInlineGap: sliderBox.left - trackBox.left,
            pillBlockGap: sliderBox.top - trackBox.top,
            selected: root.querySelector('.aeterni-segmented__option.is-selected .aeterni-segmented__label')
                ?.textContent?.trim() ?? null,
            controlRadius: token('--aeterni-radius-control-md'),
            controlHeight: token('--aeterni-control-height-md'),
            touchTargetMin: token('--aeterni-touch-target-min'),
            fullRadius: token('--aeterni-radius-full')
        };
    };
    return {
        controlled: read('形状档位预览'),
        reference: read('形状档位对照'),
        small: read('小号'),
        standard: read('默认档'),
        large: read('大号')
    };
})()`);

const roundedShape = await segmentedShape();
const rounded = roundedShape.controlled ?? {};
const fullRadius = Number.parseFloat(rounded.fullRadius ?? 'NaN');

check('Segmented 默认档取控件圆角，滑块与选项用轨道自己的圆角',
    rounded.classes?.includes('aeterni-segmented')
    && !rounded.classes.includes('aeterni-segmented--pill')
    && rounded.controlRadius === '6px'
    && rounded.radius === 6
    && rounded.radius < fullRadius
    && rounded.sliderRadiusUsed === rounded.radius
    && rounded.sliderRadiusUsed === rounded.optionRadiusUsed
    && rounded.sliderRadiusUsed === 6,
    JSON.stringify(roundedShape));

check('Segmented 默认档下选中滑块仍与选中列对齐',
    rounded.selected !== null
    && Math.abs(rounded.offset) <= 0.5
    && Math.abs(rounded.sliderWidth - rounded.optionWidth) <= 0.5,
    `selected=${rounded.selected} offset=${rounded.offset} slider=${rounded.sliderWidth} option=${rounded.optionWidth}`);

// 内缩三档统一：边框到滑块的间距不随 Size 变（都是 3px）。滑块与选项跟着该档的轨道圆角走，
// 所以圆角是 4 / 6 / 8px —— 与同档 Input / Button 用同一套控件圆角 Token。
const sizeTiers = [roundedShape.small ?? {}, roundedShape.standard ?? {}, roundedShape.large ?? {}];
check('Segmented 三档内缩统一为 3px，滑块与选项圆角跟该档轨道圆角一致',
    sizeTiers.map(tier => tier.padding).join('/') === '3/3/3'
    && sizeTiers.map(tier => tier.sliderRadiusUsed).join('/') === '4/6/8'
    && sizeTiers.every(tier => tier.sliderRadiusUsed === tier.trackRadiusUsed
        && tier.sliderRadiusUsed === tier.optionRadiusUsed
        && tier.pillInlineGap === tier.pillBlockGap),
    `tiers=${JSON.stringify(sizeTiers.map(tier => ({
        radius: tier.sliderRadiusUsed, track: tier.trackRadiusUsed, option: tier.optionRadiusUsed,
        padding: tier.padding, gap: tier.pillInlineGap
    })))}`);

// 轨道高度 = 2*描边 + 2*内缩 + 选项高度，而选项有 24px 热区下限。统一 3px 内缩之后，默认档
// （36 - 8 = 28）与大号（44 - 8 = 36）仍在热区之上，小号只剩 28 - 8 = 20px，于是被热区顶到
// 32px —— 它不再等于 --aeterni-control-height-sm，所以这条把三档高度一起钉住。
// 滑块与轨道共用同一个圆角，因此角上留白会比四边宽（三档同为 +1.66px）：这是"形状一致"的
// 代价，四边相等本身量不出它，只有沿角平分线单独量一次才看得见。
check('Segmented 三档轨道高度为 32 / 36 / 44，留白四边等宽且不缩破 24px 热区',
    sizeTiers.map(tier => tier.trackHeight).join('/') === '32/36/44'
    && rounded.trackHeight === Number.parseFloat(rounded.controlHeight)
    && rounded.optionHeight >= Number.parseFloat(rounded.touchTargetMin)
    && Math.abs(rounded.pillInlineGap - rounded.pillBlockGap) <= 0.5
    && rounded.pillInlineGap > 1
    && sizeTiers.every(tier => tier.cornerGap > tier.padding)
    && new Set(sizeTiers.map(tier => tier.cornerGap)).size === 1,
    `track=${sizeTiers.map(tier => tier.trackHeight).join('/')} (default=${rounded.trackHeight}/${rounded.controlHeight}) `
    + `option=${rounded.optionHeight}/${rounded.touchTargetMin} inline=${rounded.pillInlineGap} block=${rounded.pillBlockGap} `
    + `corner=${sizeTiers.map(tier => tier.cornerGap).join('/')} padding=${rounded.padding}`);

// 通过页面上的 Shape 控件切到 Pill（真实 ComboBox 交互，不是给类名赋值）。触发器按
// 「最近的 .aeterni-combobox 的 id」定位：ShowcaseSelect 把 id 交给 ComboBox，id 落在
// 根元素上而不是触发器按钮上，写 `[role="combobox"][id=...]` 会匹配到空集。
await evaluate(`(() => {
    const trigger = [...document.querySelectorAll('[role="combobox"]')]
        .find(node => (node.closest('.aeterni-combobox')?.id ?? '') === 'segmented-shape');
    trigger?.click();
})()`);
await sleep(100);
await evaluate(`(() => {
    const option = [...document.querySelectorAll('[role="option"]')]
        .find(element => element.textContent.trim() === 'Pill');
    option?.click();
})()`);

const shapeDeadline = Date.now() + 5000;
let pillShape = await segmentedShape();
while (Date.now() < shapeDeadline && !(pillShape.controlled?.classes ?? '').includes('aeterni-segmented--pill')) {
    await sleep(100);
    pillShape = await segmentedShape();
}
const pill = pillShape.controlled ?? {};

check('Segmented 切到 Pill 后轨道与滑块按用值半径收成半高胶囊',
    pill.classes?.includes('aeterni-segmented--pill')
    && pill.fullRadius === '9999px'
    && pill.radius === fullRadius
    && pill.radius === 9999
    && pill.trackRadiusUsed === pill.trackHeight / 2
    && pill.sliderRadiusUsed === pill.sliderHeight / 2
    && pill.sliderRadiusUsed === pill.optionRadiusUsed
    // 胶囊档不得让内缩跟着 --aeterni-segmented-inner-radius 一起变大：那会算出负值，
    // 于是 padding 落到 0、滑块被 inset 撑到两万像素高。
    && pill.padding === 3
    && Math.abs(pill.cornerGap - pill.padding) <= 0.5,
    JSON.stringify(pill));

check('Segmented 形状切换不改变滑块几何与选中项',
    pill.selected === rounded.selected
    && pill.selected !== null
    && Math.abs(pill.offset) <= 0.5
    && Math.abs(pill.sliderWidth - rounded.sliderWidth) <= 0.5
    && Math.abs(pill.optionWidth - rounded.optionWidth) <= 0.5
    && Math.abs(pill.trackWidth - rounded.trackWidth) <= 0.5,
    `rounded={offset:${rounded.offset}, slider:${rounded.sliderWidth}, option:${rounded.optionWidth}, track:${rounded.trackWidth}} `
    + `pill={offset:${pill.offset}, slider:${pill.sliderWidth}, option:${pill.optionWidth}, track:${pill.trackWidth}}`);

// 形状是实例级参数，示例把两档并排摆出来：默认档下上排是圆角、下排是胶囊，通过页面上的
// Shape 控件切到胶囊后两排反过来。所以这里同时钉住"切换只改自己那一组"（两组永远一圆角
// 一胶囊，不会互相带跑）与"没被切的那一组仍是圆角档"。
check('Segmented 形状只作用于自己那一组，切换后两组仍是相反档位',
    roundedShape.controlled?.classes.includes('aeterni-segmented--pill') === false
    && roundedShape.reference?.classes.includes('aeterni-segmented--pill') === true
    && pillShape.controlled?.classes.includes('aeterni-segmented--pill') === true
    && pillShape.reference?.classes.includes('aeterni-segmented--pill') === false
    && pillShape.reference?.radius === 6
    && pillShape.reference?.selected === pill.selected,
    `before=controlled:${roundedShape.controlled?.classes} reference:${roundedShape.reference?.classes} `
    + `after=controlled:${pillShape.controlled?.classes} reference:${pillShape.reference?.classes}`);

// ---------------------------------------------------------------- Segmented Home/End
// 原生 radio 组自带方向键与 roving tabindex，但没有 Home/End：组件在 C# 里补这一对，并用
// 一个最小模块拦掉按键的默认动作（整页滚动）。所以要连同"页面真的滚了没有"一起量——而
// 默认动作只在 raw key 事件上触发（`key()` 刻意不带它），这里另发一条 raw keydown。
//
// 用含禁用项的这组：Home/End 必须落在首/尾**可用**项上，"3 分"不参与。
await goto(`${BASE}/components/segmented`);
await waitForApp();
await waitForInteractiveTheme();
await sleep(300);

// 焦点放到当前选中项（"2 分"），并把该组滚到视口中央：只有组在视口里，"Home 有没有把
// 页面打到顶部"才是一个有分辨力的断言。
await evaluate(`(() => {
    const group = [...document.querySelectorAll('.aeterni-segmented')]
        .find(node => node.getAttribute('aria-label') === '评分区间（3 分档不可选）');
    group.scrollIntoView({ block: 'center' });
    const inputs = [...group.querySelectorAll('input')];
    (inputs.find(input => input.checked) ?? inputs[0]).focus();
})()`);

// 示例的滚动是平滑的：scrollIntoView 与 focus 都会播放动画（实测约 700ms 才停），所以
// 必须在滚动停下之后再取基准，否则读到的是动画中间值，"Home/End 有没有滚动页面"就量不准
// ——这也是这条断言第一次跑出假失败的原因（基准读到 950，实际落点 1927）。
const settleScroll = async () => {
    let last = -1;
    for (let attempt = 0; attempt < 40; attempt++) {
        const y = await evaluate('Math.round(window.scrollY)');
        if (y === last) return y;
        last = y;
        await sleep(100);
    }
    return last;
};

await settleScroll();

const segmentedKeyboard = () => evaluate(`(() => {
    const group = [...document.querySelectorAll('.aeterni-segmented')]
        .find(node => node.getAttribute('aria-label') === '评分区间（3 分档不可选）');
    if (!group) return null;
    const option = document.activeElement?.closest('.aeterni-segmented__option');
    return {
        selected: group.querySelector('.aeterni-segmented__option.is-selected')?.textContent.trim() ?? null,
        focused: option?.textContent.trim() ?? null,
        // 焦点环：option 在 :has(input:focus-visible) 下画 outline（程序化 focus 也要
        // 命中，因为触发它的是键盘）。
        ring: option ? getComputedStyle(option).outlineStyle !== 'none' : false,
        scrollY: Math.round(window.scrollY)
    };
})()`);

const rawKey = async (keyName, code, keyCode) => {
    const base = { key: keyName, code, windowsVirtualKeyCode: keyCode, nativeVirtualKeyCode: keyCode };
    await send('Input.dispatchKeyEvent', { ...base, type: 'rawKeyDown' });
    await send('Input.dispatchKeyEvent', { ...base, type: 'keyUp' });
};

const keyboardBefore = await segmentedKeyboard();
await rawKey('Home', 'Home', 36);
await sleep(200);
await settleScroll();
const keyboardHome = await segmentedKeyboard();
await rawKey('End', 'End', 35);
await sleep(200);
await settleScroll();
const keyboardEnd = await segmentedKeyboard();

check('Segmented 的 Home 跳到首个可用选项，焦点与焦点环一并过去，且不滚动页面',
    keyboardBefore?.selected === '2 分'
    && keyboardHome?.selected === '1 分'
    && keyboardHome.focused === '1 分'
    && keyboardHome.ring
    && Math.abs(keyboardHome.scrollY - keyboardBefore.scrollY) <= 2,
    `before=${JSON.stringify(keyboardBefore)} home=${JSON.stringify(keyboardHome)}`);

check('Segmented 的 End 跳到末个可用选项，且不滚动页面',
    keyboardEnd?.selected === '5 分'
    && keyboardEnd.focused === '5 分'
    && Math.abs(keyboardEnd.scrollY - keyboardBefore.scrollY) <= 2,
    `end=${JSON.stringify(keyboardEnd)}`);

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
    // 代码默认折叠：先在卡片的分段控件里选"代码"，再检查代码块与复制按钮。
    const codeOption = document.querySelector('.component-demo__toolbar .aeterni-segmented')
        ?.querySelectorAll('.aeterni-segmented__option')[1];
    codeOption?.click();
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
    const idOf = node => {
        const href = node.getAttribute('href') ?? '';
        return href.slice(href.indexOf('#') + 1);
    };
    return {
        count: links.length,
        titles: links.map(node => node.textContent.trim()),
        // 链接是"当前页 + 片段"的完整地址（写 \`#id\` 会被 <base href="/"> 解析成首页路由），
        // 因此 id 按最后一个 # 切——这条断言本身也要跟着地址形态走。
        deadLinks: links.map(idOf).filter(id => !document.getElementById(id)),
        // 页内锚点必须指向本页：出现过"点目录跳回首页"，根因就是 href 只写了片段。
        offPage: links
            .map(node => node.getAttribute('href') ?? '')
            .filter(href => href.split('#')[0] !== location.href.split('#')[0])
    };
})()`);
check('右栏目录列出本页各段，且没有死链',
    !toc.error && toc.count >= 6 && toc.deadLinks.length === 0 && toc.titles[0] === '何时使用',
    JSON.stringify(toc));
check('目录链接指向本页片段（不是首页或别的路由）',
    !toc.error && toc.offPage.length === 0,
    JSON.stringify(toc.offPage));

// 真点一次目录链接：这正是用户报过的"点右侧目录跳到首页"。断言必须落在真实点击之后，
// 而不是靠读 href 推断——Blazor 会拦截内部链接并走自己的路由。
const tocClickTarget = await evaluate(`(() => {
    const link = [...document.querySelectorAll('.component-page__toc-link')]
        .find(node => (node.getAttribute('href') ?? '').endsWith('#upload-host-progress'));
    if (!link) return { error: 'no target link' };
    link.click();
    return { clicked: true };
})()`);

// 采样点必须等到滚动停稳：平滑滚动还没走完就量位置，会把"跳到该段"误判成失败——本机实测
// 同一份代码出现过 top=245（阈值 240，FAIL）与 190（PASS）两种读数。所以轮询到位置连续两次
// 不再变化为止，而不是等一个固定时长。
async function waitForStableSection(expectedHash, deadlineMs = 5000) {
    const deadline = Date.now() + deadlineMs;
    let previousTop = null;
    let state = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const section = document.getElementById('upload-host-progress');
            return {
                path: location.pathname,
                hash: location.hash,
                // 段落到视口顶部附近即认为跳转真的发生了（页面有固定页头，留一段余量）。
                top: section ? Math.round(section.getBoundingClientRect().top) : null
            };
        })()`);
        if (state.hash === expectedHash && state.top !== null) {
            if (previousTop !== null && Math.abs(previousTop - state.top) <= 4) return state;
            previousTop = state.top;
        }
        await sleep(120);
    }
    return state;
}

const afterTocClick = await waitForStableSection('#upload-host-progress');
check('点击目录链接留在本页并滚到该段',
    !tocClickTarget.error && afterTocClick.path === '/components/upload' && afterTocClick.hash === '#upload-host-progress'
        && afterTocClick.top !== null && afterTocClick.top < 240,
    JSON.stringify({ tocClickTarget, afterTocClick }));

// 滚动到某一段：要验证的是"我的高亮逻辑"，不是浏览器的锚点跳转。
// 目标取页中的一段（下面另有断言专门覆盖页尾的段）。
await evaluate(`document.getElementById('upload-host-progress').scrollIntoView({ block: 'start' })`);

// 高亮由一个 IntersectionObserver 标记，滚动后需要等它回调（最多 4 秒）。
async function waitForCurrentSection(expected = 'upload-host-progress', deadlineMs = 4000) {
    const deadline = Date.now() + deadlineMs;
    let state = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const links = [...document.querySelectorAll('.component-page__toc-link')];
            if (links.length === 0) return { error: 'toc went away' };
            const idOf = node => {
                const href = node.getAttribute('href') ?? '';
                return href.slice(href.indexOf('#') + 1);
            };
            const current = document.querySelector('.component-page__toc-link.is-current');
            const section = document.getElementById('upload-host-progress');
            return {
                count: links.length,
                scrolled: window.scrollY > 0,
                sectionVisible: !!section && section.getBoundingClientRect().top < window.innerHeight,
                current: current ? idOf(current) : null,
                ariaCurrent: current ? current.getAttribute('aria-current') : null
            };
        })()`);
        // 必须等到"目标段"成为当前段：一进循环就返回的话，采样点会落在滚动完成之前。
        if (state && state.current === expected) return state;
        await sleep(150);
    }
    return state;
}

const tocAfterScroll = await waitForCurrentSection();
check('目录在页面滚动后仍完整（跳转/滚动不会把它弄丢）',
    tocAfterScroll && tocAfterScroll.count >= 6 && tocAfterScroll.scrolled && tocAfterScroll.sectionVisible,
    JSON.stringify(tocAfterScroll));
check('滚动到某段后高亮它（is-current + aria-current）',
    tocAfterScroll && tocAfterScroll.current === 'upload-host-progress' && tocAfterScroll.ariaCurrent === 'location',
    JSON.stringify(tocAfterScroll));

// 页尾的段永远上不到视口顶部，高亮带覆盖不到它们——原来的断言是绕开这一点的（"拿它断言等于
// 在测浏览器能不能把页尾滚到顶部"），但绕开它并不意味着读者不该在页尾看到正确的高亮。
// 这里断言修好后的行为：滚到页面底部时，最后一段（说明与边界）成为当前段。
await evaluate(`window.scrollTo(0, document.documentElement.scrollHeight)`);
const bottomState = await waitForCurrentSection('upload-notes-title');
check('滚到页面底部时高亮最后一段（相关组件 / 说明与边界不再漏选）',
    bottomState && bottomState.current === 'upload-notes-title' && bottomState.ariaCurrent === 'location',
    JSON.stringify(bottomState));

await send('Emulation.clearDeviceMetricsOverride');
await sleep(150);

// ---------------------------------------------------------------- 头部搜索入口
// 页头放的是"像输入框的按钮"，真正的搜索是一个居中弹出的模态面板：索引是生成物（页面条目 +
// 页面上真实存在的段），面板由示例自己的 DOM 与 CSS 承载，滚动锁与焦点陷阱复用库内
// `aeterni_floating.js`。断言只碰真实渲染出来的对象：触发器按 id 取，结果按 `role="option"`
// 数，落地按 `location.pathname + location.hash` 与那个 id 的元素本身。
await goto(`${BASE}/components/upload`);
await waitForApp();

// 真实指针：触发器、遮罩与结果行都是普通 click，用真实指针点一次能证明它们确实可点。
async function clickAt(x, y) {
    await send('Input.dispatchMouseEvent', { type: 'mousePressed', x, y, button: 'left', buttons: 1, clickCount: 1 });
    await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x, y, button: 'left', buttons: 0, clickCount: 1 });
}

async function clickElement(selector) {
    const point = await evaluate(`(() => {
        const node = document.querySelector('${selector}');
        if (!node) return null;
        const rect = node.getBoundingClientRect();
        return { x: Math.round(rect.left + rect.width / 2), y: Math.round(rect.top + rect.height / 2) };
    })()`);
    if (!point) return false;
    await clickAt(point.x, point.y);
    return true;
}

// Ctrl/Cmd+K：`key` 那条路不带修饰键，这条快捷键得自己发（CDP 的 modifiers 2 就是 Ctrl）。
async function pressShortcut() {
    const base = { key: 'k', code: 'KeyK', windowsVirtualKeyCode: 75, nativeVirtualKeyCode: 75, modifiers: 2 };
    await send('Input.dispatchKeyEvent', { ...base, type: 'keyDown' });
    await send('Input.dispatchKeyEvent', { ...base, type: 'keyUp' });
}

const TRIGGER = '#showcase-search-trigger';
const SEARCH_INPUT = '#showcase-header-search';
const SEARCH_LISTBOX = '#showcase-header-search-listbox';
const SEARCH_PANEL = '.showcase-search-modal__panel';

// 面板的存在就是开合状态：关着时它整个不在 DOM 里，所以"开"看节点在不在，"关"看它有没有
// 消失——不存在"打开了但看不见"的中间态。
const searchState = () => evaluate(`(() => {
    const trigger = document.querySelector('${TRIGGER}');
    const panel = document.querySelector('${SEARCH_PANEL}');
    const input = document.querySelector('${SEARCH_INPUT}');
    const listbox = document.querySelector('${SEARCH_LISTBOX}');
    const options = listbox ? [...listbox.querySelectorAll('[role="option"]')] : [];
    const rect = panel?.getBoundingClientRect() ?? null;
    return {
        triggerExists: !!trigger,
        expanded: trigger?.getAttribute('aria-expanded') ?? null,
        haspopup: trigger?.getAttribute('aria-haspopup') ?? null,
        open: !!panel,
        role: panel?.getAttribute('role') ?? null,
        modal: panel?.getAttribute('aria-modal') ?? null,
        labelled: panel?.getAttribute('aria-label') ?? null,
        scrim: !!document.querySelector('.showcase-search-modal__backdrop'),
        inputRole: input?.getAttribute('role') ?? null,
        inputExpanded: input?.getAttribute('aria-expanded') ?? null,
        controls: input?.getAttribute('aria-controls') ?? null,
        listboxPopup: input?.getAttribute('aria-haspopup') ?? null,
        activedescendant: input?.getAttribute('aria-activedescendant') ?? null,
        value: input?.value ?? null,
        focused: !!input && document.activeElement === input,
        optionCount: options.filter(option => option.getClientRects().length > 0).length,
        titles: options.map(option => option.querySelector('.showcase-search-modal__option-title')?.textContent.trim() ?? ''),
        metas: options.map(option => option.querySelector('.showcase-search-modal__option-meta')?.textContent.trim() ?? ''),
        selected: options.filter(option => option.getAttribute('aria-selected') === 'true').map(option => option.id),
        panel: rect ? { left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom } : null,
        scrollLocked: document.body.style.overflow === 'hidden',
        triggerFocused: !!trigger && document.activeElement === trigger
    };
})()`);

// 输入按"赋值 + input 事件"驱动，与上面侧栏检索那条断言同一做法：这里要断言的是过滤结果，
// 键盘路径（ArrowDown / Enter / Escape）在后面用真实按键单独覆盖。
async function typeQuery(query) {
    await evaluate(`(() => {
        const input = document.querySelector('${SEARCH_INPUT}');
        input.focus();
        input.value = ${JSON.stringify(query)};
        input.dispatchEvent(new Event('input', { bubbles: true }));
    })()`);
}

const waitForSearchState = async (predicate, timeoutMs = 5000) => {
    const deadline = Date.now() + timeoutMs;
    let state = await searchState();
    while (Date.now() < deadline) {
        if (predicate(state)) return state;
        await sleep(50);
        state = await searchState();
    }
    return state;
};

const idle = await searchState();
check('页头有搜索触发器，面板关着时它声明 dialog 契约',
    idle.triggerExists && idle.expanded === 'false' && idle.haspopup === 'dialog' && !idle.open,
    JSON.stringify(idle));
check('面板关着时不在 DOM 里，也没有遮罩与滚动锁',
    idle.panel === null && idle.scrim === false && idle.scrollLocked === false,
    JSON.stringify({ panel: idle.panel, scrim: idle.scrim, scrollLocked: idle.scrollLocked }));

await clickElement(TRIGGER);
const opened = await waitForSearchState(state => state.open && state.focused);
check('点触发器弹出模态面板：dialog 语义、遮罩、滚动锁与输入框自动聚焦',
    opened.open && opened.role === 'dialog' && opened.modal === 'true' && opened.scrim
    && opened.scrollLocked && opened.focused && opened.expanded === 'true'
    && opened.inputRole === 'combobox' && opened.inputExpanded === 'true'
    && opened.controls === SEARCH_LISTBOX.slice(1) && opened.listboxPopup === 'listbox',
    JSON.stringify(opened));

// "居中弹出"要真的居中：面板盒的中心与视口中心重合，而不是"看起来差不多"。
// 面板带入场动画，所以要等它的 rect 连续两次一致再量——动画中途量到的是插值位置
// （实测偏差会随动画进度在 -2 ~ -3px 之间浮动），那不是"没居中"。容差仍留 2px：
// 视口高为奇数时剩余空间的一半落在 .5 上，子像素取整本身就会偏 1px。
const settlePanel = async () => {
    let previous = '';
    for (let attempt = 0; attempt < 40; attempt += 1) {
        const current = await evaluate(`(() => {
            const panel = document.querySelector('${SEARCH_PANEL}');
            if (!panel) return '';
            const rect = panel.getBoundingClientRect();
            return [rect.left, rect.top, rect.right, rect.bottom].map(value => value.toFixed(2)).join(',');
        })()`);
        if (current !== '' && current === previous) return;
        previous = current;
        await sleep(50);
    }
};
await settlePanel();
const panelRect = await evaluate(`(() => {
    const panel = document.querySelector('${SEARCH_PANEL}');
    if (!panel) return null;
    const rect = panel.getBoundingClientRect();
    return { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom };
})()`);
const viewport = await evaluate(`({ w: window.innerWidth, h: window.innerHeight })`);
const centre = panelRect
    ? {
        x: Math.round((panelRect.left + panelRect.right) / 2 - viewport.w / 2),
        y: Math.round((panelRect.top + panelRect.bottom) / 2 - viewport.h / 2)
    }
    : null;
check('面板在视口里居中（水平与垂直都对上中心）',
    !!centre && Math.abs(centre.x) <= 2 && Math.abs(centre.y) <= 2,
    JSON.stringify({ panel: panelRect, viewport, centre }));

// 滚动锁：面板打开时页面不跟着滚（锁的是 body 的 overflow，视口滚动因此失效）。
const locked = JSON.parse(await evaluate(`(() => {
    window.scrollTo(0, 400);
    return JSON.stringify({ y: Math.round(window.scrollY), overflow: document.body.style.overflow });
})()`));
check('面板打开时背景不跟着滚（滚动锁生效）',
    locked.overflow === 'hidden' && locked.y === 0,
    JSON.stringify(locked));

await typeQuery('组件');
const broad = await waitForSearchState(state => state.open && state.optionCount > 3);
await typeQuery('时间');
const narrowed = await waitForSearchState(state => state.open && state.optionCount === 3);
// 命中减少到 3 条，且页码来源里同时有 Timeline 与 DateTimePicker 两页——「时间」既命中
// 段标题（时间轴、日期与时间的联合选择），也命中两页，这正是"搜索页内内容"的价值。
check('输入关键词后命中数减少且包含预期页面',
    narrowed.open && narrowed.optionCount === 3
    && narrowed.optionCount < broad.optionCount
    && narrowed.metas.some(meta => meta.startsWith('Timeline'))
    && narrowed.metas.some(meta => meta.startsWith('DateTimePicker')),
    JSON.stringify({ broad: broad.optionCount, narrowed: narrowed.optionCount, metas: narrowed.metas }));

// 结果行之间必须有间隙：每行自带圆角背景，贴在一起时活动项的底色与邻行会连成一片，读者分不出
// 那是两行还是一个色块。档位取库内 List 的同一档（--aeterni-spacing-1），列表外观因此一致。
const rowGap = await evaluate(`(() => {
    const options = [...document.querySelectorAll('${SEARCH_LISTBOX} [role="option"]')]
        .filter(option => option.getClientRects().length > 0);
    if (options.length < 2) return { gap: null, token: null };
    const first = options[0].getBoundingClientRect();
    const second = options[1].getBoundingClientRect();
    return {
        gap: Math.round((second.top - first.bottom) * 100) / 100,
        token: getComputedStyle(document.documentElement).getPropertyValue('--aeterni-spacing-1').trim()
    };
})()`);
check('搜索结果行之间留出 List 同档的间隙',
    rowGap.gap !== null && rowGap.gap >= 4 && rowGap.token === '4px',
    JSON.stringify(rowGap));

// 有查询但没命中：面板留在原地，用文案说清楚，而不是留一个空白列表。
await typeQuery('zzzz');
const noMatch = await waitForSearchState(state => state.open && state.optionCount === 0);
const emptyCopy = await evaluate(`document.querySelector('.showcase-search-modal__empty')?.textContent.trim() ?? null`);
check('没有命中时给出明确文案（面板保持打开）',
    noMatch.open && noMatch.optionCount === 0 && /没有匹配/.test(emptyCopy ?? ''),
    JSON.stringify({ ...noMatch, emptyCopy }));

await typeQuery('时间');
await waitForSearchState(state => state.open && state.optionCount === 3);
await key('ArrowDown', 'ArrowDown', 40);
const moved = await waitForSearchState(state => state.activedescendant === 'showcase-header-search-option-1');
check('ArrowDown 把活动项移到第二条（循环列表里的下一项）',
    moved.activedescendant === 'showcase-header-search-option-1'
    && moved.selected.length === 1
    && moved.selected[0] === moved.activedescendant
    && moved.titles[1] === '横向时间轴',
    JSON.stringify({ active: moved.activedescendant, selected: moved.selected, titles: moved.titles }));

await press('Enter', 'Enter', 13, '\r');
// 落地要等两件事：路由换到目标页，以及跨页 hash 的滚动（框架只在同文档内自己滚）。
const landed = await (async () => {
    const deadline = Date.now() + 8000;
    let state = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const hash = location.hash.slice(1);
            const target = hash ? document.getElementById(hash) : null;
            const rect = target?.getBoundingClientRect() ?? null;
            return {
                route: location.pathname + location.hash,
                targetExists: !!target,
                inViewport: !!rect && rect.top > -1 && rect.top < window.innerHeight,
                panel: !!document.querySelector('${SEARCH_PANEL}'),
                input: !!document.querySelector('${SEARCH_INPUT}'),
                overflow: document.body.style.overflow
            };
        })()`);
        if (state.route.includes('timeline') && state.inViewport) return state;
        await sleep(100);
    }
    return state;
})();
check('Enter 跳到该段：路由、锚点元素与滚动都到位',
    landed.route === '/components/timeline#timeline-horizontal' && landed.targetExists && landed.inViewport,
    JSON.stringify(landed));
// 跳走时焦点留给落地页：这时把焦点还给页头触发器，会和刚滚到的目标段脱节。
// 解锁是"渲染之后"的那次互操作，比路由到位晚一点点：等它落下来再断言。
const settled = await (async () => {
    const deadline = Date.now() + 5000;
    let state = null;
    while (Date.now() < deadline) {
        state = JSON.parse(await evaluate(`JSON.stringify({
            panel: !!document.querySelector('${SEARCH_PANEL}'),
            input: !!document.querySelector('${SEARCH_INPUT}'),
            overflow: document.body.style.overflow
        })`));
        if (!state.panel && !state.input && state.overflow === '') return state;
        await sleep(50);
    }
    return state;
})();
check('命中后面板与输入框都离场、滚动锁释放',
    !settled.panel && !settled.input && settled.overflow === '',
    JSON.stringify(settled));

// Escape 收起面板：面板整个消失，键盘用户得有个落脚点——焦点回到触发器。
await clickElement(TRIGGER);
const reopened = await waitForSearchState(state => state.open && state.focused);
check('每次打开都是一次干净的搜索（空查询、无命中、焦点在输入框）',
    reopened.open && reopened.focused && reopened.value === '' && reopened.optionCount === 0,
    JSON.stringify(reopened));

await key('Escape', 'Escape', 27);
const escaped = await waitForSearchState(state => !state.open);
check('Escape 收起面板、释放滚动锁并把焦点还给触发器',
    !escaped.open && !escaped.scrollLocked && escaped.triggerFocused,
    JSON.stringify(escaped));

// Ctrl+K 是同一条开合路径的第二个入口：关着时打开，开着时收起。
await pressShortcut();
const shortcutOpened = await waitForSearchState(state => state.open && state.focused);
check('Ctrl+K 打开面板', shortcutOpened.open && shortcutOpened.focused, JSON.stringify(shortcutOpened));

await pressShortcut();
const shortcutClosed = await waitForSearchState(state => !state.open);
check('面板已打开时 Ctrl+K 收起它', !shortcutClosed.open && shortcutClosed.triggerFocused,
    JSON.stringify(shortcutClosed));

// Tab 停在面板里：模态面板不能把焦点 Tab 到背后的页头或正文上（面板里只有输入框与关闭按钮两个
// 停留点，所以这条也顺带证明焦点确实在环上绕）。
await clickElement(TRIGGER);
await waitForSearchState(state => state.open && state.focused);
const focusTrail = [];
let trapped = true;
for (let index = 0; index < 4; index++) {
    await key('Tab', 'Tab', 9);
    const step = await evaluate(`(() => {
        const panel = document.querySelector('${SEARCH_PANEL}');
        const active = document.activeElement;
        return {
            inside: !!panel && !!active && panel.contains(active),
            label: active?.getAttribute('aria-label') ?? active?.id ?? active?.tagName ?? null
        };
    })()`);
    trapped = trapped && step.inside;
    focusTrail.push(step.label);
}
check('Tab 焦点留在面板内（焦点陷阱生效）', trapped, JSON.stringify(focusTrail));

// 点遮罩关闭：取面板之外、视口之内的一点（它落在铺满视口的遮罩上，不是页头也不是正文）。
const scrimPoint = await evaluate(`(() => {
    const panel = document.querySelector('${SEARCH_PANEL}');
    const rect = panel.getBoundingClientRect();
    const x = Math.round(Math.max(8, rect.left - 80));
    const y = Math.round(rect.top + rect.height / 2);
    return {
        x, y,
        insidePanel: x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom,
        viewport: { w: window.innerWidth, h: window.innerHeight }
    };
})()`);
if (!scrimPoint.insidePanel) {
    await clickAt(scrimPoint.x, scrimPoint.y);
}

const dismissed = await waitForSearchState(state => !state.open);
check('点遮罩关闭面板、释放滚动锁并把焦点还给触发器',
    !scrimPoint.insidePanel && !dismissed.open && !dismissed.scrollLocked && dismissed.triggerFocused,
    JSON.stringify({ ...dismissed, scrimPoint }));

// ---------------------------------------------------------------- 头部设置浮层
// 主题与品牌两个开关从页头并排搬进浮层：触发器带 aria-haspopup / aria-expanded，
// 关闭后焦点归还触发器（RestoreFocusOnClose）。
const SETTINGS_TRIGGER = '#showcase-settings-button';
await evaluate(`document.querySelector('${SETTINGS_TRIGGER}')?.focus()`);
await evaluate(`document.querySelector('${SETTINGS_TRIGGER}')?.click()`);
const settingsOpen = await (async () => {
    const deadline = Date.now() + 5000;
    let state = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const trigger = document.querySelector('${SETTINGS_TRIGGER}');
            const panel = document.querySelector('#showcase-settings-panel');
            const popover = panel?.closest('.aeterni-popover') ?? null;
            const groups = panel ? [...panel.querySelectorAll('[role="radiogroup"]')] : [];
            return {
                exists: !!trigger,
                expanded: trigger?.getAttribute('aria-expanded') ?? null,
                haspopup: trigger?.getAttribute('aria-haspopup') ?? null,
                open: popover ? !popover.hasAttribute('hidden') : null,
                groups: groups.length,
                labels: groups.map(group => group.getAttribute('aria-label')),
                groupLabels: panel ? [...panel.querySelectorAll('.showcase-settings__label')].map(n => n.textContent.trim()) : []
            };
        })()`);
        if (state.expanded === 'true' && state.open) return state;
        await sleep(50);
    }
    return state;
})();
check('设置入口声明 aria-haspopup / aria-expanded 并打开浮层',
    settingsOpen.exists && settingsOpen.haspopup === 'true' && settingsOpen.expanded === 'true' && settingsOpen.open,
    JSON.stringify(settingsOpen));
check('设置浮层里有品牌色与主题模式两个开关',
    settingsOpen.groups === 2 && settingsOpen.labels.includes('品牌色') && settingsOpen.labels.includes('主题模式')
    && settingsOpen.groupLabels.join(',') === '品牌色,主题模式',
    JSON.stringify(settingsOpen));

// 焦点先落进浮层里的开关，再按 Escape：关闭路径要把焦点还给触发器。
await evaluate(`document.querySelector('#showcase-settings-panel input[type="radio"]')?.focus()`);
await key('Escape', 'Escape', 27);
const settingsClosed = await (async () => {
    const deadline = Date.now() + 5000;
    let state = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const trigger = document.querySelector('${SETTINGS_TRIGGER}');
            const panel = document.querySelector('#showcase-settings-panel');
            const popover = panel?.closest('.aeterni-popover') ?? null;
            return {
                expanded: trigger?.getAttribute('aria-expanded') ?? null,
                open: popover ? !popover.hasAttribute('hidden') : null,
                focused: document.activeElement === trigger
            };
        })()`);
        if (state.expanded === 'false' && state.open === false) return state;
        await sleep(50);
    }
    return state;
})();
check('Escape 关闭设置浮层并让 aria-expanded 回到 false',
    settingsClosed.expanded === 'false' && settingsClosed.open === false,
    JSON.stringify(settingsClosed));
check('关闭后焦点归还设置触发器', settingsClosed.focused, JSON.stringify(settingsClosed));

// ------------------------------------------------- 搜索面板在页头玻璃卡片之外
// 面板必须渲染在页头那张玻璃卡片之外：卡片的 `backdrop-filter` 会成为最近的 backdrop root，
// 卡内浮层的模糊就只能采样卡片内部那张透明背景。断言顺序是"先根因、后现象"：先证明面板与页面
// 根之间没有别的 backdrop root（否则采样范围根本不是整页），再看面板表面确实带着模糊，最后量
// 几何（居中、铺满视口的遮罩、压在页头之上）。
await goto(`${BASE}/components/upload`);
await waitForApp();
await clickElement(TRIGGER);
const anchored = await waitForSearchState(state => state.open && state.focused);
await typeQuery('时间');
await waitForSearchState(state => state.open && state.optionCount === 3);

// 遍历面板表面到页面根之间的祖先：任何一个带 `backdrop-filter` 的祖先都会成为最近的
// backdrop root，面板就只能采样那一块。面板表面自己带模糊，所以从它的父元素开始走。
const blurChain = await evaluate(`(() => {
    const surface = document.querySelector('${SEARCH_PANEL}');
    const page = document.querySelector('.sample-page');
    if (!surface || !page) return { error: 'no panel or page root' };
    const chain = [];
    for (let node = surface.parentElement; node && node !== page.parentElement; node = node.parentElement) {
        const style = getComputedStyle(node);
        // 类名会随排序变化，取最能定位的一段：组件根类或元素名。
        const label = (node.getAttribute('class') || node.tagName).split(/\\s+/).slice(0, 2).join('.');
        chain.push({ label, backdrop: style.backdropFilter, transform: style.transform });
        if (node === page) break;
    }
    return {
        chain,
        own: getComputedStyle(surface).backdropFilter,
        roots: chain.filter(entry => entry.backdrop && entry.backdrop !== 'none').map(entry => entry.label)
    };
})()`);
check('搜索面板与页面根之间没有别的 backdrop root（模糊采样的是整页）',
    !blurChain.error && anchored.open && blurChain.roots.length === 0,
    JSON.stringify(blurChain));
check('搜索面板表面自身带模糊', !blurChain.error && /blur\(/.test(blurChain.own ?? ''),
    JSON.stringify({ own: blurChain.own }));

// 这里不再做"模糊确实作用在背后页面内容上"的像素锐度对比：那段采样当初是为了证明卡内浮层的
// 背投是空的（面板在卡片盒之外，backdrop root 却是那张卡片）。现在面板与页面根之间已经没有别的
// backdrop root，而采样区里同时还有模态遮罩自己的 scrim 模糊，锐度差无法再单独归因于面板这一层
// ——与其留一条归因不明的断言，不如把"祖先链干净 + 面板自身带模糊"两条留在上面。

// 几何：固定定位、铺满视口的遮罩、面板压在页头那张玻璃卡片之上（面板渲染在卡片之外，也必须
// 盖住它，否则页头会浮在遮罩上）。
const panelGeometry = await evaluate(`(() => {
    const box = node => { const r = node.getBoundingClientRect(); return { left: r.left, right: r.right, top: r.top, bottom: r.bottom, width: r.width, height: r.height }; };
    const panel = document.querySelector('${SEARCH_PANEL}');
    const scrim = document.querySelector('.showcase-search-modal__backdrop');
    const modal = document.querySelector('.showcase-search-modal');
    const card = document.querySelector('.sample-header .aeterni-surface');
    const header = document.querySelector('.sample-header');
    if (!panel || !scrim || !modal || !card || !header) return { error: 'missing nodes' };
    return {
        scrim: box(scrim),
        insideCard: card.contains(panel) || card.contains(modal),
        position: getComputedStyle(modal).position,
        panelZ: Number(getComputedStyle(modal).zIndex),
        headerZ: Number(getComputedStyle(header).zIndex),
        viewport: { w: window.innerWidth, h: window.innerHeight }
    };
})()`);
check('搜索面板是固定定位、渲染在页头玻璃卡片之外',
    !panelGeometry.error && panelGeometry.position === 'fixed' && !panelGeometry.insideCard,
    JSON.stringify({ position: panelGeometry.position, insideCard: panelGeometry.insideCard }));
check('面板压在页头之上（层级高于那张玻璃卡片）',
    !panelGeometry.error && panelGeometry.panelZ > panelGeometry.headerZ,
    JSON.stringify({ panelZ: panelGeometry.panelZ, headerZ: panelGeometry.headerZ }));
check('遮罩铺满整页视口',
    !panelGeometry.error
    && Math.abs(panelGeometry.scrim.left) <= 1 && Math.abs(panelGeometry.scrim.top) <= 1
    && Math.abs(panelGeometry.scrim.width - panelGeometry.viewport.w) <= 1
    && Math.abs(panelGeometry.scrim.height - panelGeometry.viewport.h) <= 1,
    JSON.stringify({ scrim: panelGeometry.scrim, viewport: panelGeometry.viewport }));

// 关闭要真的把滚动锁还回去：模态自己开完不还，页面就此锁死（开锁那半条在上面，这条盯释放）。
await key('Escape', 'Escape', 27);
const released = await waitForSearchState(state => !state.open);
await evaluate('window.scrollTo(0, 400)');
await sleep(200);
const scrolledAfterClose = await evaluate('Math.round(window.scrollY)');
check('关闭面板后滚动锁释放、页面重新可滚',
    !released.open && !released.scrollLocked && scrolledAfterClose > 0,
    JSON.stringify({ ...released, scrolledAfterClose }));
await evaluate('window.scrollTo(0, 0)');
await sleep(100);

// 设置浮层：同一套锚定，锚点是齿轮按钮。
await evaluate(`document.querySelector('${SETTINGS_TRIGGER}')?.focus()`);
await evaluate(`document.querySelector('${SETTINGS_TRIGGER}')?.click()`);
const settingsAnchored = await (async () => {
    const deadline = Date.now() + 5000;
    let state = null;
    let previous = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const trigger = document.querySelector('${SETTINGS_TRIGGER}');
            const layer = document.querySelector('.showcase-settings__popover');
            const surface = layer?.querySelector('.aeterni-popover__surface');
            const card = document.querySelector('.sample-header .aeterni-surface');
            const box = node => { const r = node.getBoundingClientRect(); return { left: r.left, right: r.right, top: r.top, bottom: r.bottom }; };
            if (!trigger || !layer || !surface) return { error: 'missing nodes' };
            return {
                open: !layer.hasAttribute('hidden'),
                position: getComputedStyle(layer).position,
                anchored: layer.classList.contains('is-anchored'),
                rightDelta: Math.round((box(layer).right - box(trigger).right) * 100) / 100,
                topGap: Math.round((box(surface).top - box(trigger).bottom) * 100) / 100,
                cardGap: Math.round((box(surface).top - box(card).bottom) * 100) / 100,
                hostInsideCard: card.contains(layer.parentElement)
            };
        })()`);
        // 入场动画会把面板从 -4px 处推进来：连续两次量到同一组数值才算真的落位，否则会量到
        // 动画中途的位置（-1.26 这种值正好落在"看起来还行"的区间里，把断言变成碰运气）。
        if (!state.error && state.open && state.anchored
            && previous && previous.topGap === state.topGap && previous.cardGap === state.cardGap) {
            return state;
        }
        previous = state;
        await sleep(60);
    }
    return state;
})();
check('设置浮层同样锚定在齿轮上：右边缘齐平、贴在按钮下方、卡片之外',
    !settingsAnchored.error && Math.abs(settingsAnchored.rightDelta) <= 2
    && settingsAnchored.topGap > 0 && settingsAnchored.topGap <= 24
    && settingsAnchored.cardGap >= -0.5
    && settingsAnchored.position === 'fixed'
    && !settingsAnchored.hostInsideCard,
    JSON.stringify(settingsAnchored));
await key('Escape', 'Escape', 27);
await evaluate(`document.querySelector('${SETTINGS_TRIGGER}')?.blur()`);

// 回归：不设 Anchor 的既有 Popover 仍定位在它自己的 PopupHost 里（绝对定位、贴着宿主盒），
// 示例其它页面继续用这一形态。
await goto(`${BASE}/components/popup`);
await waitForApp();
const plainBefore = await evaluate(`(() => {
    const layer = document.querySelector('#popup-basic .aeterni-popover') ?? document.querySelector('.aeterni-popover');
    if (!layer) return { error: 'no popover' };
    return {
        position: getComputedStyle(layer).position,
        anchored: layer.classList.contains('is-anchored'),
        hostIsParent: layer.parentElement.classList.contains('aeterni-popup-host'),
        open: !layer.hasAttribute('hidden')
    };
})()`);
check('不设 Anchor 的 Popover 保持宿主内绝对定位（无锚点状态）',
    !plainBefore.error && plainBefore.position === 'absolute' && !plainBefore.anchored
    && plainBefore.hostIsParent && plainBefore.open === false,
    JSON.stringify(plainBefore));

await evaluate(`document.querySelector('#popup-basic button[aria-label="打开 Popover 浮层"]')?.click()`);
const plainOpen = await (async () => {
    const deadline = Date.now() + 5000;
    let state = null;
    while (Date.now() < deadline) {
        state = await evaluate(`(() => {
            const layer = document.querySelector('#popup-basic .aeterni-popover');
            const host = layer?.parentElement;
            if (!layer || !host) return { error: 'no popover' };
            const gap = parseFloat(getComputedStyle(layer).getPropertyValue('--aeterni-popover-gap')) || 0;
            const box = node => { const r = node.getBoundingClientRect(); return { left: r.left, right: r.right, top: r.top, bottom: r.bottom }; };
            return {
                open: !layer.hasAttribute('hidden'),
                position: getComputedStyle(layer).position,
                anchored: layer.classList.contains('is-anchored'),
                // 方位可能是 BottomStart，也可能因为视口没地方而翻到 TopStart：两种都合法，
                // 但必须是"贴宿主盒"的那一种。
                bottomAligned: Math.round((box(layer).top - (box(host).bottom + gap)) * 100) / 100,
                topAligned: Math.round((box(host).top - gap - box(layer).bottom) * 100) / 100,
                leftDelta: Math.round((box(layer).left - box(host).left) * 100) / 100,
                inlineLeft: layer.style.left,
                inlineTop: layer.style.top
            };
        })()`);
        if (!state.error && state.open) return state;
        await sleep(50);
    }
    return state;
})();
check('不设 Anchor 的 Popover 打开后仍贴着自己的宿主盒（横向对齐、纵向留 8px 间距）',
    !plainOpen.error && plainOpen.position === 'absolute' && !plainOpen.anchored
    && plainOpen.inlineLeft === '' && plainOpen.inlineTop === ''
    && Math.abs(plainOpen.leftDelta) <= 1
    && Math.min(Math.abs(plainOpen.bottomAligned), Math.abs(plainOpen.topAligned)) <= 1,
    JSON.stringify(plainOpen));
await key('Escape', 'Escape', 27);

// 卡片化页面：一页多段 demo，每段自带锚点、说明与代码。先重新导航一次——折叠态说的是初始
// 状态，而上面的壳检查已经切到过"代码"。
await goto(`${BASE}/components/upload`);
await waitForApp();

//这是市面组件文档的页面模板，
// 也是批次 3 的可见成果——此前每页只有一段演示。
const cards = await evaluate(`(() => {
    const sections = [...document.querySelectorAll('.component-demo')];
    return {
        count: sections.length,
        anchors: sections.map(node => node.id),
        // 预览 / 代码由库内 Segmented 承担：每张有代码的卡片一个分段控件。
        codeSwitches: sections.filter(node => node.querySelector('.component-demo__toolbar .aeterni-segmented')).length,
        // 代码默认折叠：代码面板仍带 hidden。注意不能数 DOM 里的代码块——querySelectorAll
        // 照样会数到隐藏面板内部，那样断言永远是"折叠失败"。
        openCodePanels: [...document.querySelectorAll('.component-demo__panel--code')]
            .filter(panel => !panel.hasAttribute('hidden')).length,
        when: !!document.querySelector('.component-page__when'),
        related: document.querySelectorAll('.component-page__related a').length,
        api: document.querySelectorAll('.component-preview__api-row').length,
        edit: document.querySelector('.component-page__edit')?.getAttribute('href') ?? null
    };
})()`);
check('组件页按 demo 卡片组织，每段带锚点与"代码"切换',
    cards.count >= 3 && cards.codeSwitches === cards.count && cards.anchors.every(anchor => anchor.length > 0),
    JSON.stringify(cards));
check('代码默认折叠在分段控件后面', cards.openCodePanels === 0, JSON.stringify(cards));

// 分段控件的开关必须真的切换面板：原生 radio 的 change 事件 → Blazor 重渲染这条路径，
// 静态检查看不见。点"代码"后代码面板要摘掉 hidden、预览面板要带上，控件自己也停在新档。
await evaluate(`(() => {
    const root = document.querySelector('.component-demo__toolbar .aeterni-segmented');
    root?.querySelectorAll('.aeterni-segmented__option')[1]?.click();
})()`);
await sleep(300);
const toggled = await evaluate(`(() => {
    const code = document.querySelector('.component-demo__panel--code');
    const preview = document.querySelector('.component-demo__panel--preview');
    return {
        codeHidden: code?.hasAttribute('hidden') ?? null,
        previewHidden: preview?.hasAttribute('hidden') ?? null,
        checked: document.querySelector('.component-demo__toolbar .aeterni-segmented__input:checked')
            ?.closest('.aeterni-segmented__option')?.textContent?.trim() ?? null
    };
})()`);
check('分段控件切到「代码」后面板真的换过来',
    toggled.codeHidden === false && toggled.previewHidden === true && toggled.checked === '代码',
    JSON.stringify(toggled));
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

// 批次 5：非组件页（安装、设计基础、对比实验）与组件页共用同一套页面模板。以前它们直接渲染
// 进布局壳、只有「在 GitHub 编辑」一个入口，段锚点与搜索结果因此与其余页面不同源；这里断言
// 三段固定段、可复制代码块与右栏目录都由骨架给出，页面上没有第二套模板。
await goto(`${BASE}/components`);
await waitForApp();
const installTemplate = await evaluate(`(() => {
    const q = s => document.querySelector(s);
    return {
        heading: q('.component-preview__header h1')?.textContent?.trim() ?? null,
        when: !!q('.component-page__when'),
        related: document.querySelectorAll('.component-page__related a').length,
        notes: !!q('.component-preview__notes'),
        copies: document.querySelectorAll('.component-preview__copy').length,
        toc: document.querySelectorAll('.component-page__toc-link').length,
        api: document.querySelectorAll('.component-preview__api-row').length
    };
})()`);
check('非组件页（安装）也按同一模板渲染：何时使用、相关组件、说明与边界与四个可复制代码块',
    installTemplate.heading === '安装' && installTemplate.when && installTemplate.related >= 3
    && installTemplate.notes && installTemplate.copies === 4 && installTemplate.toc === 3
    && installTemplate.api === 0,
    JSON.stringify(installTemplate));

// 设计基础与实验台两页同理：规格表 / 实验舞台住在 Demos 段里，骨架负责标题区与三段，
// 两页各带两段可复制代码块，且都没有 API 表（它们记录的不是单个组件）。
for (const path of ['/components/colors', '/components/glass']) {
    await goto(`${BASE}${path}`);
    await waitForApp();
    const template = await evaluate(`(() => {
        const q = s => document.querySelector(s);
        return {
            when: !!q('.component-page__when'),
            related: document.querySelectorAll('.component-page__related a').length,
            notes: !!q('.component-preview__notes'),
            copies: document.querySelectorAll('.component-preview__copy').length,
            api: document.querySelectorAll('.component-preview__api-row').length,
            body: !!q('.component-page__demos section')
        };
    })()`);
    check(`非组件页 ${path}：骨架 + 两段可复制代码块，内容仍在自己的一节里`,
        template.when && template.related >= 3 && template.notes && template.copies >= 2
        && template.api === 0 && template.body,
        JSON.stringify(template));
}

// 卡片化的覆盖面：单独抽一两页断言不够——批次 3 的目标是整组页面都变成"多段 demo +
// 每段代码"，所以按组分批抽查，任何一页退回单段形态都会在这里失败。
for (const path of ['/components/input', '/components/checkbox', '/components/radio', '/components/multi-select']) {
    await goto(`${BASE}${path}`);
    await waitForApp();
    const shape = await evaluate(`(() => {
        const sections = [...document.querySelectorAll('.component-demo')];
        return {
            cards: sections.length,
            codeSwitches: sections.filter(node => node.querySelector('.component-demo__toolbar .aeterni-segmented')).length,
            uniqueAnchors: new Set(sections.map(node => node.id)).size,
            when: !!document.querySelector('.component-page__when')
        };
    })()`);
    check(`卡片化 ${path}：多段 demo、锚点唯一、每段带代码切换、有何时使用`,
        shape.cards >= 3 && shape.codeSwitches === shape.cards && shape.uniqueAnchors === shape.cards && shape.when,
        JSON.stringify(shape));
}

// 没有对应组件类型的页（反馈页记录的是 IDialogService 与各个选项类型）同样要有 API 区：
// 它的行全部来自生成器的额外行。以前门禁把这页当例外，于是它带着三张没人消费的数组通过了
// 检查，而页面上根本没有 API 区——这是"例外"最典型的后果，所以在这里补一条真实渲染断言。
await goto(`${BASE}/components/feedback`);
await waitForApp();
const feedbackApi = await evaluate(`(() => {
    const rows = [...document.querySelectorAll('.component-preview__api-row')];
    return {
        rows: rows.length,
        heading: !!document.querySelector('#feedback-api-title'),
        service: rows.filter(row => row.textContent.includes('IDialogService')).length,
        provider: rows.filter(row => row.textContent.includes('DialogProvider')).length
    };
})()`);
check('无组件类型的页（反馈）仍渲染生成式 API 表',
    feedbackApi.rows >= 18 && feedbackApi.heading && feedbackApi.service >= 1 && feedbackApi.provider >= 1,
    JSON.stringify(feedbackApi));

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

// 组合用法页（批次 6）：这些页记录的不是单个组件，而是把若干组件串成一条链，所以断言落在
// "链条真的连上了"的地方——提交被拦下时消息落到该字段、输入时立刻标红、失焦才给结论。
// 这里的选择器必须排除分段控件的 radio：每张有代码的卡片都带一个"预览/代码"开关。
await goto(`${BASE}/components/form-validation`);
await waitForApp();
const formChain = await evaluate(`(() => {
    const section = document.querySelector('#form-validation-submit');
    return {
        cards: document.querySelectorAll('.component-demo').length,
        copies: document.querySelectorAll('.component-preview__copy').length,
        errorsBefore: section ? section.querySelectorAll('.aeterni-form-field__error').length : null
    };
})()`);
check('组合用法页（表单校验链）：三段 demo 各带可复制代码，初始没有任何错误',
    formChain.cards === 3 && formChain.copies === 3 && formChain.errorsBefore === 0,
    JSON.stringify(formChain));

await evaluate(`document.querySelector('#form-validation-submit button[type=submit]')?.click()`);
await sleep(500);
const blocked = await evaluate(`(() => {
    const section = document.querySelector('#form-validation-submit');
    const input = section.querySelector('input:not([type=radio])');
    return {
        message: section.querySelector('.aeterni-form-field__error')?.textContent?.trim() ?? null,
        invalid: input?.getAttribute('aria-invalid') ?? null,
        describedBy: input?.getAttribute('aria-describedby') ?? null,
        feedback: section.querySelector('.preview-feedback')?.textContent?.trim() ?? null
    };
})()`);
check('提交时校验：错误落在该字段，控件带 aria-invalid 与 aria-describedby',
    blocked.message === '项目名称必填。' && blocked.invalid === 'true'
    && (blocked.describedBy ?? '').length > 0
    && (blocked.feedback ?? '').includes('有字段未通过校验'),
    JSON.stringify(blocked));

await evaluate(`(() => {
    const input = document.querySelector('#form-validation-submit input:not([type=radio])');
    input.value = 'Aeterni Web';
    input.dispatchEvent(new Event('input', { bubbles: true }));
})()`);
await sleep(300);
await evaluate(`document.querySelector('#form-validation-submit button[type=submit]')?.click()`);
await sleep(500);
const passed = await evaluate(`(() => {
    const section = document.querySelector('#form-validation-submit');
    return {
        errors: section.querySelectorAll('.aeterni-form-field__error').length,
        feedback: section.querySelector('.preview-feedback')?.textContent?.trim() ?? null
    };
})()`);
check('提交时校验：补齐必填后提交通过，字段错误消失',
    passed.errors === 0 && (passed.feedback ?? '').includes('已提交'),
    JSON.stringify(passed));

await evaluate(`(() => {
    const input = document.querySelector('#form-validation-live input:not([type=radio])');
    input.value = 'not-an-email';
    input.dispatchEvent(new Event('input', { bubbles: true }));
})()`);
await sleep(300);
const liveInvalid = await evaluate(`(() => {
    const section = document.querySelector('#form-validation-live');
    return { invalid: !!section.querySelector('.aeterni-form-field.is-invalid') };
})()`);
check('输入时校验：非法邮箱在输入过程中就被标红', liveInvalid.invalid, JSON.stringify(liveInvalid));

await evaluate(`(() => {
    const input = document.querySelector('#form-validation-blur input:not([type=radio])');
    input.value = 'A';
    input.focus();
    input.dispatchEvent(new Event('input', { bubbles: true }));
})()`);
await sleep(300);
const typingQuiet = await evaluate(`(() => {
    const section = document.querySelector('#form-validation-blur');
    return { invalid: !!section.querySelector('.aeterni-form-field.is-invalid') };
})()`);
await evaluate(`document.querySelector('#form-validation-blur input:not([type=radio])')?.blur()`);
await sleep(400);
const afterBlur = await evaluate(`(() => {
    const section = document.querySelector('#form-validation-blur');
    return {
        invalid: !!section.querySelector('.aeterni-form-field.is-invalid'),
        message: section.querySelector('.aeterni-form-field__error')?.textContent?.trim() ?? null
    };
})()`);
check('失焦时校验：输入过程中不报错，离开字段后给出结论',
    typingQuiet.invalid === false && afterBlur.invalid
    && (afterBlur.message ?? '').includes('只能用小写字母'),
    JSON.stringify({ typingQuiet, afterBlur }));

// 筛选工具栏：条件归宿主，派生结果只有一份；已选条件是同一份状态的下沉呈现
await goto(`${BASE}/components/filter-toolbar`);
await waitForApp();
const toolbarShape = await evaluate(`(() => {
    const basic = document.querySelector('#filter-toolbar-basic');
    return {
        cards: document.querySelectorAll('.component-demo').length,
        tags: basic.querySelectorAll('.preview-state-row .aeterni-tag').length,
        note: basic.querySelector('.preview-feedback')?.textContent?.trim() ?? null
    };
})()`);
check('组合用法页（筛选工具栏）：初始列出全部记录，工具栏给出计数',
    toolbarShape.cards === 3 && toolbarShape.tags === 12 && (toolbarShape.note ?? '').includes('12 / 12'),
    JSON.stringify(toolbarShape));

await evaluate(`(() => {
    const input = document.querySelector('#filter-toolbar-basic input:not([type=radio]):not([role=combobox])');
    input.value = '网关';
    input.dispatchEvent(new Event('input', { bubbles: true }));
})()`);
await sleep(400);
const toolbarFiltered = await evaluate(`(() => {
    const basic = document.querySelector('#filter-toolbar-basic');
    return {
        tags: [...basic.querySelectorAll('.preview-state-row .aeterni-tag')].map(node => node.textContent.trim()),
        note: basic.querySelector('.preview-feedback')?.textContent?.trim() ?? null,
        chips: [...document.querySelectorAll('#filter-toolbar-chips .aeterni-tag')].map(node => node.textContent.trim())
    };
})()`);
check('筛选工具栏：关键词筛掉其余记录，同一份条件在「已选条件」里变成可关闭标签',
    toolbarFiltered.tags.length === 1 && (toolbarFiltered.note ?? '').includes('1 / 12')
    && toolbarFiltered.chips.some(text => text.includes('关键词：网关')),
    JSON.stringify(toolbarFiltered));

// 设置抽屉：草稿语义——放弃改动不碰生效值，页面上的对比行因此必须一字不变
await goto(`${BASE}/components/settings-drawer`);
await waitForApp();
await evaluate(`(() => {
    const open = [...document.querySelectorAll('#settings-drawer-draft button')]
        .find(node => node.textContent.trim() === '打开通知设置');
    open?.click();
})()`);
await sleep(500);
const drawerModal = await evaluate(`(() => {
    const root = document.querySelector('#settings-drawer-draft .aeterni-drawer');
    const panel = root?.querySelector('.aeterni-drawer__panel');
    return {
        open: !!root?.classList.contains('is-open'),
        role: panel?.getAttribute('role') ?? null,
        ariaModal: panel?.getAttribute('aria-modal') ?? null,
        backdrop: !!root?.querySelector('.aeterni-drawer__backdrop')
    };
})()`);
check('组合用法页（设置抽屉）：打开后进入模态语义（dialog + aria-modal + 遮罩）',
    drawerModal.open && drawerModal.role === 'dialog'
    && drawerModal.ariaModal === 'true' && drawerModal.backdrop,
    JSON.stringify(drawerModal));

const savedBefore = await evaluate(`document.querySelector('#settings-drawer-draft .preview-feedback')?.textContent?.trim() ?? null`);
await evaluate(`document.querySelector('#settings-drawer-draft .aeterni-drawer__panel input[role=switch]')?.click()`);
await sleep(300);
await evaluate(`(() => {
    const button = [...document.querySelectorAll('#settings-drawer-draft .aeterni-drawer__footer button')]
        .find(node => node.textContent.trim() === '放弃改动');
    button?.click();
})()`);
await sleep(500);
const discarded = await evaluate(`(() => {
    const section = document.querySelector('#settings-drawer-draft');
    return {
        open: !!section.querySelector('.aeterni-drawer.is-open'),
        note: section.querySelector('.preview-feedback')?.textContent?.trim() ?? null
    };
})()`);
check('设置抽屉：放弃改动只丢草稿，「当前生效」一行一个字都没变',
    discarded.open === false && discarded.note === savedBefore,
    JSON.stringify({ savedBefore, discarded }));

// 通知与长任务：取消要停在当前百分比并给出回执，而不是继续在后台跑
await goto(`${BASE}/components/long-tasks`);
await waitForApp();
await evaluate(`(() => {
    const button = [...document.querySelectorAll('#long-tasks-progress button')]
        .find(node => node.textContent.trim() === '开始同步');
    button?.click();
})()`);
await sleep(900);
await evaluate(`(() => {
    const button = [...document.querySelectorAll('#long-tasks-progress button')]
        .find(node => node.textContent.trim() === '取消');
    button?.click();
})()`);
await sleep(700);
const cancelled = await evaluate(`(() => {
    const section = document.querySelector('#long-tasks-progress');
    return {
        note: section.querySelector('.preview-feedback')?.textContent?.trim() ?? null,
        value: Number(section.querySelector('[role=progressbar]')?.getAttribute('aria-valuenow') ?? -1)
    };
})()`);
check('组合用法页（通知与长任务）：取消后停在当前百分比并给出「已取消」回执',
    (cancelled.note ?? '').includes('已取消') && cancelled.value > 0 && cancelled.value < 100,
    JSON.stringify(cancelled));

// 大数据列表：800 条只渲染视口附近的行；多选由宿主保存，批量动作条按选中与否出现
await goto(`${BASE}/components/data-list`);
await waitForApp();
const dataList = await evaluate(`(() => {
    const search = document.querySelector('#data-list-search');
    return {
        rendered: search.querySelectorAll('.aeterni-virtual-list__viewport > *').length,
        note: search.querySelector('.preview-feedback')?.textContent?.trim() ?? null,
        toolbar: !!document.querySelector('#data-list-batch [role="toolbar"], #data-list-batch .aeterni-toolbar')
    };
})()`);
check('组合用法页（大数据列表）：800 条只渲染视口附近的几十行，未选中时不出现批量动作条',
    dataList.rendered > 0 && dataList.rendered <= 32
    && (dataList.note ?? '').includes('命中 800 / 800') && dataList.toolbar === false,
    JSON.stringify(dataList));

// 虚拟列表的行要等渲染出来才可点；而且导航之后 Blazor 可能还有一批渲染没落地，
// 这时发出去的第一次点击会落空（CI 上表现为"已选 0 项"，本机却总是通过）。
// 所以"点一下、查一次选中数"，不中就再点——重试的是动作送达，不是被断言的行为。
const clickRowUntilSelected = async (index, expectedSelected) => {
    for (let attempt = 0; attempt < 30; attempt += 1) {
        await evaluate(`document.querySelectorAll('#data-list-selection [role="option"]')[${index}]?.click()`);
        await sleep(150);
        const selected = await evaluate(`document.querySelectorAll('#data-list-selection [role="option"][aria-selected="true"]').length`);
        if (selected === expectedSelected) return true;
    }
    return false;
};
const firstRowSelected = await clickRowUntilSelected(0, 1);
if (!firstRowSelected) {
    console.error('first row never reported as selected');
}
await clickRowUntilSelected(1, 2);
const selection = await evaluate(`(() => {
    const section = document.querySelector('#data-list-selection');
    const batch = document.querySelector('#data-list-batch');
    return {
        note: section.querySelector('.preview-feedback')?.textContent?.trim() ?? null,
        selectedRows: section.querySelectorAll('[role="option"][aria-selected="true"]').length,
        toolbar: !!batch.querySelector('[role="toolbar"], .aeterni-toolbar')
    };
})()`);
check('大数据列表：多选计数与行选中态一致，选中后批量动作条才出现',
    (selection.note ?? '').includes('已选 2 项') && selection.selectedRows === 2 && selection.toolbar,
    JSON.stringify(selection));

// 整站体检：每个页面都要把源码里声明的 demo 段全部渲染出来，且不出现未处理异常。
// 这一类缺陷编译期完全看不见，症状都是"页面静静地少一段"：属性值里的裸引号会被 Razor 截断
// （`Avatar` 的「图片与回退」就这么消失了很久），值类型绑定的表达式多一层 `Convert` 会被
// `FieldIdentifier` 拒绝（`InputNumber` 的 EditForm 那一段整页中断）。段数是目录、右栏 TOC
// 与搜索索引共同的事实源，少一段就意味着三处一起对不上。
await send('Page.addScriptToEvaluateOnNewDocument', {
    source: `
        window.__renderErrors = [];
        const record = (level, args) => {
            try {
                const text = args.map(value => (value && value.stack) || String(value)).join(' ');
                if (/crit:|Unhandled|does not have a property|UnaryExpression|InvalidOperationException/i.test(text)) {
                    window.__renderErrors.push(level + ': ' + text.slice(0, 220));
                }
            } catch { }
        };
        const originalError = console.error;
        const originalWarn = console.warn;
        console.error = (...args) => { record('error', args); originalError.apply(console, args); };
        console.warn = (...args) => { record('warn', args); originalWarn.apply(console, args); };
    `
});

const declaredCards = readdirSync(SHOWCASE_PAGES)
    .filter(name => name.endsWith('.razor'))
    .map(name => {
        const source = readFileSync(new URL(name, SHOWCASE_PAGES), 'utf8');
        return [
            /@page "([^"]+)"/.exec(source)?.[1] ?? null,
            [...source.matchAll(/<DemoCard\s+Id="([^"]+)"/g)].map(match => match[1])
        ];
    })
    .filter(([route]) => route !== null);

const auditProblems = [];
for (const [route, cards] of declaredCards) {
    await goto(`${BASE}${route}`).catch(() => { });
    await waitForApp(20000).catch(() => { });
    await sleep(200);

    const state = await evaluate(`(() => ({
        demos: [...document.querySelectorAll('.component-demo')].map(node => node.id),
        errors: window.__renderErrors ?? []
    }))()`).catch(() => null);

    if (state === null) {
        auditProblems.push(`${route}: 读不到页面状态`);
        continue;
    }

    const missing = cards.filter(id => !state.demos.includes(id));
    if (missing.length > 0 || state.errors.length > 0) {
        auditProblems.push(`${route}: 缺 ${missing.join('/') || '无'}；异常 ${state.errors.length} 条`);
    }
}

check('整站体检：每个页面都把源码里声明的 demo 段全部渲染出来，且没有未处理异常',
    auditProblems.length === 0 && declaredCards.length >= 50,
    auditProblems.slice(0, 6).join(' | '));

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

// 连体接缝由 ButtonGroup 与 SplitButton 共用一档，因此两者不会再各自漂移：透明档取
// 按钮自身强调色的 45%，中性墨色（alpha .78）渲染出来约 0.351。10.34.3 之前这一档
// 被 SplitButton 单独写死成 20%（实际 0.156），在页面背景上几乎读不出是两段连体。
const alphaOf = value => {
    const match = String(value).match(/([0-9]*\.?[0-9]+)\s*\)\s*$/);
    return match ? Number(match[1]) : null;
};

await goto(`${BASE}/components/split-button`);
await waitForApp();
const seam = JSON.parse(await evaluate(`JSON.stringify((() => {
    const split = document.querySelector('.aeterni-button--split-menu');
    if (!split) return { error: 'no split menu segment' };
    const style = getComputedStyle(split);
    return {
        className: split.className,
        seam: style.borderInlineStartColor,
        transparent: /aeterni-button--(outline|soft|ghost|link)/.test(split.className)
    };
})())`));
const seamAlpha = alphaOf(seam.seam);
check('SplitButton 与 ButtonGroup 共用同一档连体缝，透明档是 45% 而不是自己的 20%',
    !seam.error && seam.transparent && seamAlpha !== null && seamAlpha >= 0.30 && seamAlpha <= 0.40,
    `seam=${seam.seam} alpha=${seamAlpha}`);

console.log(`\n${results.filter(r => r.ok).length}/${results.length} checks passed`);
const failed = results.some(r => !r.ok);
console.log(failed ? 'RESULT: FAIL' : 'RESULT: PASS');
close();
// Without this the harness printed FAIL and still exited 0, so `run.sh` (and the
// CI browser job) reported success while checks were broken.
if (failed) {
    process.exitCode = 1;
}
