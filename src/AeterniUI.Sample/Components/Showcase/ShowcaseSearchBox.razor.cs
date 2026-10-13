using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 头部常驻搜索框的触发器：持有输入框与它的包裹元素（浮层锚点）。
/// </summary>
/// <remarks>
/// 状态、过滤与键盘语义都在 <see cref="ShowcaseSearchModel" /> 里——结果浮层渲染在页头玻璃
/// 卡片之外，两半必须看同一份状态。这里只做三件事：把模型状态铺到 Input 上、把包裹元素与
/// 输入框元素交出去、在模型变化时重渲染。
/// </remarks>
public partial class ShowcaseSearchBox : ComponentBase, IDisposable
{
    [Inject]
    private ShowcaseSearchModel Model { get; set; } = default!;

    private ElementReference _anchor;
    private ElementReference _inputElement;

    /// <summary>
    /// 输入框上的 combobox 契约。`Input` 自己会写 aria-label 与字段状态，这几个键由调用方补，
    /// 与库内 Autocomplete 传给自己的 `Input` 的那一组一致。
    /// </summary>
    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "combobox",
                ["aria-expanded"] = Model.Open ? "true" : "false",
                ["aria-controls"] = ShowcaseSearchModel.ListboxId,
                ["aria-haspopup"] = "listbox",
                ["aria-autocomplete"] = "list"
            };

            if (Model.ActiveOptionId is { } active)
            {
                attributes["aria-activedescendant"] = active;
            }

            return attributes;
        }
    }

    protected override void OnInitialized() => Model.Changed += HandleModelChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Model.SetAnchor(_anchor);
        if (Model.Open)
        {
            await Model.SyncActiveOptionAsync(_inputElement);
        }
    }

    private void HandleQueryChanged(string? value) => Model.SetQuery(value);

    private void HandleKeyDown(KeyboardEventArgs args) => Model.HandleKey(args);

    private void HandleInputElementChanged(ElementReference element) => _inputElement = element;

    private void HandleModelChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Model.Changed -= HandleModelChanged;
}
