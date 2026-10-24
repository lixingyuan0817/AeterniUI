// 页头搜索的三组 DOM 侧职责。
//
//   1. 打开/关闭居中面板时的模态行为：页面滚动锁、Tab 焦点陷阱、焦点进入输入框；关闭时释放
//      这两者并把焦点还给触发器（或按调用方要求留给落地页）。三者全部复用库内共享模块
//      `aeterni_floating.js`——组件浮层用的是同一份，示例不另造一套。
//   2. 全局快捷键 Ctrl/Cmd+K：键盘事件只有挂在 document 上、且在任何焦点位置都拦得住时才
//      成立，而 Blazor 的处理器绑在元素上，所以这一条只能在这里注册，再把意图回调给 C#。
//   3. 输入框上的两件小事：导航键的默认动作（Home/End 会把光标挪到行首行尾，ArrowUp/Down 在
//      地址栏式的历史里也各有含义，这些默认动作发生在浏览器里，`@onkeydown` 只能拿到事件、
//      拦不住默认动作），以及活动项的滚动（结果条目用 `aria-activedescendant` 表达活动项，
//      它不会获得焦点，浏览器不会替它滚动）。与库内 Autocomplete.razor.js 同一套做法。
//
// 跨页跳转落到段锚点也在这里：框架只在"同文档"（路径与查询串都不变）时自己滚动，从别的页面
// 跳过来时它只 `history.pushState`，浏览器不会滚动；落地时目标页可能还没渲染出那一段。
// 其余一切（数据、过滤、活动项、ARIA、路由、开合状态）都在 C# 里。

import { createFocusTrap, lockScroll } from '../_content/AeterniUI/js/aeterni_floating.js';

const navigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape']);

// 打开一次面板就是一个"会话"：面板与输入框元素、焦点陷阱与滚动锁的释放函数、焦点回归目标。
// 示例只有一个搜索入口，模块级单例足够，也省得 C# 侧再传一个实例键。
let session = null;
let shortcut = null;

/**
 * 打开面板：上滚动锁、装焦点陷阱、把焦点交给输入框，并绑定输入框的键位。
 *
 * 输入框按 id 解析，不靠 Blazor 传元素引用：父组件的 `OnAfterRenderAsync` 先于子组件执行，
 * 面板第一次挂载时 `Input` 的 `ElementChanged` 还没回填，元素引用那一趟必然是空的。
 * 同一个面板重复调用只做"重新绑定 + 滚动活动项"，不重建陷阱——面板打开期间每次输入都会
 * 渲染一次，重建会反复抢焦点。
 */
export function mountPanel(panel, inputId, trigger) {
    if (!panel) return;

    const input = inputId ? document.getElementById(inputId) : null;

    if (session?.panel === panel) {
        // 输入框是同一个元素就什么都不抢；换过元素（重渲染换节点）时焦点回落到它身上。
        const rebound = session.input !== input;
        bindInput(input);
        if (rebound && input) input.focus();
        scrollActiveIntoView(input);
        return;
    }

    releaseSession();

    session = {
        panel,
        input: null,
        inputHandler: null,
        trigger,
        releaseTrap: createFocusTrap(panel, { initialFocus: false, restoreFocus: false }).release,
        releaseScroll: lockScroll()
    };

    bindInput(input);
    // 陷阱的 initialFocus 会落在面板里第一个可聚焦元素上，而这里要的是输入框本身：焦点陷阱
    // 只管 Tab 的边界，进入面板的落点由调用方决定。
    input?.focus?.();
    scrollActiveIntoView(input);
}

/**
 * 关闭面板：释放滚动锁与焦点陷阱。`restoreFocus` 为真时把焦点还给触发器——键盘用户在面板
 * 消失后必须有个落脚点；从结果跳走的场景则留给落地页，不与目标段抢焦点。
 */
export function unmountPanel(restoreFocus = true) {
    const trigger = session?.trigger;
    releaseSession();

    if (restoreFocus && trigger?.isConnected) {
        trigger.focus?.();
    }
}

/** 活动项变化后的滚动：面板打开期间由 C# 在每次渲染后调用。 */
export function sync() {
    if (!session) return;

    scrollActiveIntoView(session.input);
}

function releaseSession() {
    if (!session) return;

    const current = session;
    session = null;

    if (current.input && current.inputHandler) {
        current.input.removeEventListener('keydown', current.inputHandler);
    }

    current.releaseTrap?.();
    current.releaseScroll?.();
}

function bindInput(nextInput) {
    if (session.input === nextInput && session.inputHandler) return;

    if (session.input && session.inputHandler) {
        session.input.removeEventListener('keydown', session.inputHandler);
    }

    session.input = nextInput ?? null;
    session.inputHandler = null;

    if (!nextInput) return;

    const handler = event => {
        if (event.target !== nextInput || nextInput.disabled) return;
        if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        if (event.isComposing) return;

        // 输入框只出现在打开的面板里，所以它总是"展开"的：导航键与 Enter 的默认动作一律让给它。
        if (navigationKeys.has(event.key) || event.key === 'Enter') {
            event.preventDefault();
        }
    };

    session.inputHandler = handler;
    nextInput.addEventListener('keydown', handler);
}

function scrollActiveIntoView(activeInput) {
    const active = document.getElementById(activeInput?.getAttribute('aria-activedescendant'));
    if (!active) return;

    const viewport = active.parentElement;
    const row = active.getBoundingClientRect();
    const box = viewport.getBoundingClientRect();
    if (row.top < box.top) viewport.scrollTop -= box.top - row.top;
    else if (row.bottom > box.bottom) viewport.scrollTop += row.bottom - box.bottom;
}

function isApplePlatform() {
    const source = navigator.userAgentData?.platform ?? navigator.platform ?? navigator.userAgent ?? '';
    return /mac|iphone|ipad|ipod/i.test(source);
}

/**
 * 全局快捷键：面板关着时打开，开着时关闭。`kbd` 是触发器上的键位提示，按平台写成 ⌘K 或 Ctrl K。
 */
export function attachShortcut(dotNet, kbd) {
    detachShortcut();

    if (kbd) {
        kbd.textContent = isApplePlatform() ? '⌘K' : 'Ctrl K';
    }

    const handler = event => {
        if (!event.ctrlKey && !event.metaKey) return;
        if (event.altKey || event.shiftKey) return;
        if ((event.key ?? '').toLowerCase() !== 'k') return;

        // 浏览器自己也把 Ctrl/Cmd+K 当入口（地址栏搜索），必须拦下来才轮得到面板。
        event.preventDefault();
        // 触发器释放后（组件正在卸载）没有接收方：这次按键不能留下未处理的 rejection。
        dotNet.invokeMethodAsync(session ? 'CloseFromShortcut' : 'OpenFromShortcut').catch(() => { });
    };

    document.addEventListener('keydown', handler, true);
    shortcut = { dotNet, handler };
}

/** 组件释放时摘掉全局监听；重挂时先摘旧的，避免一次按键回调两次。 */
export function detachShortcut() {
    if (!shortcut) return;

    document.removeEventListener('keydown', shortcut.handler, true);
    shortcut = null;
}

// 页面全局是 `scroll-behavior: smooth`；系统要求减少动态效果时，段锚点跳转必须直接落位，
// 不能把读者拖进一段几百毫秒的滚动动画。滚动行为在调用点显式给出，不依赖 CSS 是否被覆盖。
const scrollBehavior = () =>
    window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth';

// 等目标段出现再滚：跨页导航刚落地时目标页还没渲染出该段，直接滚会滚到一个还不存在的元素。
// 超时返回 false，调用方据此知道这次没落上（不静默假装成功）。
export function scrollToAnchor(anchor, timeoutMs = 3000) {
    return new Promise(resolve => {
        const deadline = performance.now() + timeoutMs;
        const attempt = () => {
            const target = document.getElementById(anchor);
            if (target) {
                target.scrollIntoView({ block: 'start', behavior: scrollBehavior() });
                resolve(true);
                return;
            }

            if (performance.now() > deadline) {
                resolve(false);
                return;
            }

            window.requestAnimationFrame(attempt);
        };

        attempt();
    });
}
