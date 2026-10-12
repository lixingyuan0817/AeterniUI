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
