using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 页头搜索入口的触发器：把面板打开，并挂上全局快捷键。
/// </summary>
/// <remarks>
/// 状态、过滤与键盘语义都在 <see cref="ShowcaseSearchModel" /> 里——面板渲染在页头玻璃卡片
/// 之外，两半必须看同一份状态。这里只做三件事：把触发器元素交出去（面板关闭后焦点回到它）、
/// 在首次渲染后挂上全局快捷键、在模型变化时重渲染。
/// </remarks>
public partial class ShowcaseSearchBox : ComponentBase, IDisposable
{
    [Inject]
    private ShowcaseSearchModel Model { get; set; } = default!;

    private ElementReference _trigger;
    private ElementReference _kbd;
    private DotNetObjectReference<ShowcaseSearchBox>? _reference;

    /// <summary>Ctrl/Cmd+K 打开面板（JS 按当前状态决定调本方法还是下面的关闭入口）。</summary>
    [JSInvokable]
    public void OpenFromShortcut() => Model.OpenPanel();

    /// <summary>Ctrl/Cmd+K 在面板已经打开时收起它。</summary>
    [JSInvokable]
    public void CloseFromShortcut() => Model.ClosePanel();

    protected override void OnInitialized() => Model.Changed += HandleModelChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Model.SetTrigger(_trigger);

        if (firstRender)
        {
            _reference = DotNetObjectReference.Create(this);
            await Model.AttachShortcutAsync(_reference, _kbd);
        }
    }

    private void HandleModelChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Model.Changed -= HandleModelChanged;
        _reference?.Dispose();
        _ = Model.DetachShortcutAsync();
    }
}
