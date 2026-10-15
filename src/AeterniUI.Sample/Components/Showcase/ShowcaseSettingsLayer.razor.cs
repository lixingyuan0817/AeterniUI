using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 头部外观设置的浮层：与 <see cref="ShowcaseSettings" /> 共享 <see cref="ShowcaseSettingsModel" />，
/// 但渲染在页头玻璃卡片之外，按齿轮按钮的包裹元素定位。
/// </summary>
public partial class ShowcaseSettingsLayer : ComponentBase, IDisposable
{
    [Inject]
    private ShowcaseSettingsModel Model { get; set; } = default!;

    [Inject]
    private ShowcasePreviewModel Preview { get; set; } = default!;

    protected override void OnInitialized()
    {
        Model.Changed += HandleModelChanged;
        Preview.Changed += HandlePreviewChanged;
    }

    private void HandleModelChanged() => InvokeAsync(StateHasChanged);

    private void HandlePreviewChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Model.Changed -= HandleModelChanged;
        Preview.Changed -= HandlePreviewChanged;
    }
}
