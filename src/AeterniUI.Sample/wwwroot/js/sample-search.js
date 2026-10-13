// 头部搜索框的两件 DOM 侧职责。
//
//   1. 导航键的默认动作：Home/End 会把光标挪到行首行尾，ArrowUp/ArrowDown 在地址栏式的历史
//      里也各有含义，这些默认动作发生在浏览器里，`@onkeydown` 只能拿到事件、拦不住默认动作。
//      活动项的滚动同样只能在这里做：结果条目用 `aria-activedescendant` 表达活动项，它不会
//      获得焦点，浏览器不会替它滚动。与库内 Autocomplete.razor.js 同一套做法。
//   2. 跨页跳转落到段锚点：框架只在"同文档"（路径与查询串都不变）时自己滚动，从别的页面跳
//      过来时它只 `history.pushState`，浏览器不会滚动。所以要自己滚一次，并且要等目标页把该
//      段渲染出来——`pushState` 之后目标元素还不存在。
// 其余一切（数据、过滤、活动项、ARIA、路由）都在 C# 里。

const navigationKeys = new Set(['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape']);

let input = null;
let inputHandler = null;

export function sync(nextInput) {
    if (input === nextInput && inputHandler) {
        scrollActiveIntoView(input);
        return;
    }

    input?.removeEventListener('keydown', inputHandler);
    input = nextInput;
    inputHandler = null;

    if (!input) return;

    scrollActiveIntoView(input);

    inputHandler = event => {
        if (event.target !== input || input.disabled) return;
        if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
        if (event.isComposing) return;

        const expanded = input.getAttribute('aria-expanded') === 'true';
        const navigation = expanded && navigationKeys.has(event.key);
        if (navigation || (expanded && event.key === 'Enter')) {
            event.preventDefault();
        }
    };
    input.addEventListener('keydown', inputHandler);
}

function scrollActiveIntoView(activeInput) {
    const active = document.getElementById(activeInput.getAttribute('aria-activedescendant'));
    if (!active) return;

    const viewport = active.parentElement;
    const row = active.getBoundingClientRect();
    const box = viewport.getBoundingClientRect();
    if (row.top < box.top) viewport.scrollTop -= box.top - row.top;
    else if (row.bottom > box.bottom) viewport.scrollTop += row.bottom - box.bottom;
}

// 等目标段出现再滚：跨页导航刚落地时目标页还没渲染出该段，直接滚会滚到一个还不存在的元素。
// 超时返回 false，调用方据此知道这次没落上（不静默假装成功）。
export function scrollToAnchor(anchor, timeoutMs = 3000) {
    return new Promise(resolve => {
        const deadline = performance.now() + timeoutMs;
        const attempt = () => {
            const target = document.getElementById(anchor);
            if (target) {
                target.scrollIntoView({ block: 'start' });
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
