using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Components.Showcase;

public partial class ComponentPreview : ComponentBase
{
    [Parameter, EditorRequired]
    public string AnchorId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Category { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string ComponentName { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Description { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment Preview { get; set; } = default!;

    [Parameter]
    public RenderFragment? Controls { get; set; }

    [Parameter]
    public RenderFragment? Notes { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseParameter>? Parameters { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseEvent>? Events { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseContract>? Contracts { get; set; }

    private IReadOnlyList<ShowcaseParameter> EffectiveParameters =>
        CommonParameters.Concat(Parameters ?? []).ToArray();

    private static readonly ShowcaseParameter[] CommonParameters =
    [
        new("Id", "string?", "覆盖根元素的 DOM id。"),
        new("Disabled", "bool", "组件是否处于禁用状态；仅支持该语义的组件会应用它。", "false"),
        new("Visible", "bool", "是否渲染组件；false 时输出 hidden。", "true"),
        new("Class", "string?", "追加到根元素的 class。"),
        new("Style", "string?", "追加到根元素的 inline style。"),
        new("Element", "ElementReference?", "提供或接收根元素引用。"),
        new("AdditionalAttributes", "IReadOnlyDictionary<string, object>?", "透传未匹配的 HTML 属性。"),
        new("ElementChanged", "EventCallback<ElementReference>", "根元素引用发生变化时触发。")
    ];
}

public sealed record ShowcaseParameter(
    string Name,
    string Type,
    string Description,
    string? DefaultValue = null);

public sealed record ShowcaseEvent(
    string Name,
    string Signature,
    string Description);

public sealed record ShowcaseContract(
    string Name,
    string Type,
    string Description);
