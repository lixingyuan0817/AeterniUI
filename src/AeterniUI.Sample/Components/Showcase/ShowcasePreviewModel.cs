namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 页头「预览状态」的共享状态：RTL 方向、减少动效与窄屏三个示例级开关。
/// </summary>
/// <remarks>
/// 开关放在外观设置浮层里，但效果作用在整站外壳（<c>.sample-page</c> 的 <c>dir</c> 与修饰类），
/// 因此生命周期跟随应用（scoped），由布局订阅并在变化时重渲染——浮层与外壳两个视图读同一份状态。
/// </remarks>
public sealed class ShowcasePreviewModel
{
    /// <summary>任一开关变化后触发：设置浮层与布局据此重渲染。</summary>
    public event Action? Changed;

    private bool _rtl;
    private bool _reducedMotion;
    private bool _narrow;

    public bool Rtl => _rtl;

    public bool ReducedMotion => _reducedMotion;

    public bool Narrow => _narrow;

    public void SetRtl(bool on) => Set(ref _rtl, on);

    public void SetReducedMotion(bool on) => Set(ref _reducedMotion, on);

    public void SetNarrow(bool on) => Set(ref _narrow, on);

    private void Set(ref bool field, bool value)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Changed?.Invoke();
    }
}
