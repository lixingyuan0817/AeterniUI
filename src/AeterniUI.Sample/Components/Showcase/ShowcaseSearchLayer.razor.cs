using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 居中搜索面板：与 <see cref="ShowcaseSearchBox" /> 共享 <see cref="ShowcaseSearchModel" />，
/// 但渲染在页头玻璃卡片之外，并且只在打开时进入 DOM。
/// </summary>
/// <remarks>
/// 打开/关闭时的浏览器侧动作（滚动锁、焦点陷阱、焦点进出）都走模型的互操作入口——面板在
/// DOM 里的进出只有这里知道，所以挂载与卸载都在渲染之后触发：先渲染出面板，再上陷阱。
/// </remarks>
public partial class ShowcaseSearchLayer : ComponentBase, IDisposable
{
    [Inject]
    private ShowcaseSearchModel Model { get; set; } = default!;

    private ElementReference _panel;
    private bool _mounted;

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
                // 输入框只存在于打开的面板里，所以它总是展开的：面板关掉就整个从 DOM 里消失，
                // 不存在"输入框还在、列表已收起"的中间状态。
                ["aria-expanded"] = "true",
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
        if (Model.Open)
        {
            // 面板打开期间每次渲染都会走到这里：JS 侧对同一个面板是幂等的，只重新解析输入框
            // 并滚动活动项，不重建焦点陷阱（重建会反复抢焦点）。
            _mounted = true;
            await Model.MountPanelAsync(_panel);
            await Model.SyncActiveOptionAsync();
        }
        else if (_mounted)
        {
            _mounted = false;
            await Model.UnmountPanelAsync();
        }
    }

    private string OptionClass(int index) =>
        index == Model.ActiveIndex ? "showcase-search-modal__option is-active" : "showcase-search-modal__option";

    private void HandleQueryChanged(string? value) => Model.SetQuery(value);

    private void HandleInputKeyDown(KeyboardEventArgs args) => Model.HandleKey(args);

    /// <summary>Escape 在面板里的任何位置都收起它（焦点在输入框或关闭按钮上都算）。</summary>
    private void HandlePanelKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            Model.ClosePanel();
        }
    }

    private void HandleModelChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Model.Changed -= HandleModelChanged;
}
