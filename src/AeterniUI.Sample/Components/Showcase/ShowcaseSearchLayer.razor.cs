using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 头部搜索的结果浮层：与 <see cref="ShowcaseSearchBox" /> 共享 <see cref="ShowcaseSearchModel" />，
/// 但渲染在页头玻璃卡片之外，按触发器的包裹元素定位。
/// </summary>
public partial class ShowcaseSearchLayer : ComponentBase, IDisposable
{
    [Inject]
    private ShowcaseSearchModel Model { get; set; } = default!;

    protected override void OnInitialized() => Model.Changed += HandleModelChanged;

    private string OptionClass(int index) =>
        index == Model.ActiveIndex ? "showcase-search-box__option is-active" : "showcase-search-box__option";

    private void HandleModelChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Model.Changed -= HandleModelChanged;
}
