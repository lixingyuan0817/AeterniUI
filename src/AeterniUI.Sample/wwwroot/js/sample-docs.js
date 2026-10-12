// 文档壳里唯一需要 JavaScript 的行为：复制示例代码。
//
// 放在应用级脚本而不是组件模块里：一则一个页面上有多段代码，事件委托比给每个
// ComponentPreview 实例挂模块省事；二则剪贴板写入是浏览器能力，不需要 C# 参与，
// 也就不必为一次复制付一次 interop。
//
// 非安全上下文（例如用 http://<ip> 打开示例）里 navigator.clipboard 不存在或会被
// 拒绝，这时退回 document.execCommand('copy')。两条路径都失败时按钮显示“复制失败”：
// 一个谎报成功的复制按钮比一个报错的更糟。
document.addEventListener('click', async event => {
    const target = event.target;
    if (!(target instanceof Element)) return;

    const button = target.closest('[data-sample-copy]');
    if (!button) return;

    const block = document.getElementById(button.dataset.sampleCopy);
    if (!block) return;

    const copied = await writeClipboard(block.textContent ?? '');
    const label = button.textContent;
    button.textContent = copied ? '已复制' : '复制失败';
    button.dataset.sampleCopyState = copied ? 'copied' : 'failed';
    window.setTimeout(() => {
        button.textContent = label;
        delete button.dataset.sampleCopyState;
    }, 2000);
});

async function writeClipboard(text) {
    if (navigator.clipboard?.writeText) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            // 权限被拒绝或非安全上下文：继续走降级路径。
        }
    }

    try {
        const area = document.createElement('textarea');
        area.value = text;
        area.setAttribute('readonly', '');
        area.style.position = 'fixed';
        area.style.top = '-1000px';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        const copied = document.execCommand('copy');
        document.body.removeChild(area);
        return copied;
    } catch {
        return false;
    }
}

// ---------------------------------------------------------------- 页内目录的滚动高亮
//
// 放在应用级脚本里而不是组件模块：示例组件不接库内的 JsModuleManager（它们不是
// AeterniComponent），而且 Blazor 会整棵子树重渲染，目录元素随时可能被替换——绑定做成
// "发现即（重新）绑定"最稳：MutationObserver 只负责在目录出现或换掉时重绑，滚动高亮本身
// 用 IntersectionObserver。这是 JS 该用的地方（滚动观察），其余仍在 C#。
let boundToc = null;
let sectionObserver = null;
let bindScheduled = false;

function bindToc() {
    bindScheduled = false;
    const toc = document.querySelector('.component-page__toc');
    if (toc === boundToc) return;

    sectionObserver?.disconnect();
    sectionObserver = null;
    boundToc = toc;
    if (!toc) return;

    // 高亮带取视口上三分之一：一段进入这条带就认为"正在读它"，向上滚回去同样成立。
    sectionObserver = new IntersectionObserver(entries => {
        for (const entry of entries) {
            if (!entry.isIntersecting) continue;
            setCurrent(toc, entry.target.id);
        }
    }, { rootMargin: '-15% 0px -70% 0px' });

    for (const link of toc.querySelectorAll('.component-page__toc-link')) {
        const id = (link.getAttribute('href') ?? '').slice(1);
        const section = id ? document.getElementById(id) : null;
        if (section) sectionObserver.observe(section);
    }
}

function setCurrent(toc, id) {
    for (const link of toc.querySelectorAll('.component-page__toc-link')) {
        const current = (link.getAttribute('href') ?? '') === `#${id}`;
        link.classList.toggle('is-current', current);
        if (current) link.setAttribute('aria-current', 'location');
        else link.removeAttribute('aria-current');
    }
}

function scheduleBind() {
    if (bindScheduled) return;
    bindScheduled = true;
    window.requestAnimationFrame(bindToc);
}

new MutationObserver(scheduleBind).observe(document.documentElement, { childList: true, subtree: true });
document.addEventListener('DOMContentLoaded', scheduleBind);
