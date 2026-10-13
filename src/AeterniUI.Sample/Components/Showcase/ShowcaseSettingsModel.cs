using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 页头外观设置入口的共享状态：开合与浮层锚点。
/// </summary>
/// <remarks>
/// 与搜索框同一个形状（触发器在页头的玻璃卡片里、浮层渲染在卡片之外），只是状态更小：
/// 触发器要读 <see cref="Open" /> 写 <c>aria-expanded</c>，浮层要读同一份开合状态并按
/// <see cref="Anchor" /> 定位。
/// </remarks>
public sealed class ShowcaseSettingsModel
{
    /// <summary>状态变化后触发：触发器与浮层两个视图据此重渲染。</summary>
    public event Action? Changed;

    /// <summary>触发器包裹元素：浮层以它为锚点定位，也把它算作"层内"。</summary>
    public ElementReference? Anchor { get; private set; }

    public bool Open { get; private set; }

    public void Toggle()
    {
        Open = !Open;
        Notify();
    }

    /// <summary>Popover 只在 Escape、点击外部与滚动时请求关闭。</summary>
    public void SetOpen(bool open)
    {
        Open = open;
        Notify();
    }

    /// <summary>把锚点元素交给浮层；元素没变时不打扰两个视图。</summary>
    public void SetAnchor(ElementReference anchor)
    {
        // `ElementReference` 没有相等运算符，按它的元素 id 比较：同一个元素重渲染后 id 不变。
        if (Anchor is { } current && string.Equals(current.Id, anchor.Id, StringComparison.Ordinal))
        {
            return;
        }

        Anchor = anchor;
        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
