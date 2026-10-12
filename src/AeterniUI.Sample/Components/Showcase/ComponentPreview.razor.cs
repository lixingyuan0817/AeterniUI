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
    public string? PreviewClass { get; set; }

    [Parameter]
    public RenderFragment? Controls { get; set; }

    [Parameter]
    public RenderFragment? Notes { get; set; }

    /// <summary>
    /// Razor usage sample, rendered as a copyable code block.
    /// </summary>
    /// <remarks>
    /// The snippet is the shape a consumer copies, not a transcript of the demo
    /// above: the demo drives a dozen parameters through its controls, while a
    /// snippet that long teaches nothing. Every parameter named here has to exist
    /// on the component, and the sample gate asserts it.
    /// </remarks>
    [Parameter]
    public string? Code { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseParameter>? Parameters { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseEvent>? Events { get; set; }

    [Parameter]
    public IReadOnlyList<ShowcaseContract>? Contracts { get; set; }

    private IReadOnlyList<ShowcaseParameter> EffectiveParameters =>
        CommonParameters.Concat(Parameters ?? []).ToArray();

    /// <summary>
    /// Rows generated from the library source for this page, used when the page declares no
    /// hand-written table. A page that still passes <see cref="Parameters"/> or
    /// <see cref="Events"/> keeps rendering its own rows, so the migration can proceed page by
    /// page without a half-converted build.
    /// </summary>
    private IReadOnlyList<ShowcaseApiMember> GeneratedMembers =>
        Parameters is null && Events is null && ShowcaseApiReference.ByPage.TryGetValue(AnchorId, out var rows)
            ? rows
            : [];

    private IReadOnlyList<ShowcaseApiMember> GeneratedContracts =>
        Contracts is null && ShowcaseApiReference.Contracts.TryGetValue(AnchorId, out var rows)
            ? rows
            : [];

    /// <summary>
    /// The base parameters every component inherits, in the generated table's shape. They are
    /// one shared list documented once in <c>delivered-features</c>, so they are merged in at
    /// render time instead of being repeated for all 53 pages in the generated artifact.
    /// </summary>
    /// <remarks>
    /// A computed property rather than a static field: <c>CommonParameters</c> is declared
    /// further down the file, and a static initializer would read it while it is still null.
    /// Eight rows per render is not worth the trap.
    /// </remarks>
    private static IReadOnlyList<ShowcaseApiMember> BaseMembers => CommonParameters
        .Select(parameter => new ShowcaseApiMember(
            parameter.Name,
            parameter.Type,
            parameter.DefaultValue ?? string.Empty,
            parameter.Description,
            "属性",
            string.Empty))
        .ToArray();

    /// <summary>Whether this page documents an API at all.</summary>
    private bool HasApiSection =>
        GeneratedGroups.Count > 0 || Parameters is not null || Events is not null || Contracts is not null;

    /// <summary>
    /// Generated rows grouped for rendering, in reading order. Empty groups are dropped: an
    /// empty table with a heading reads like a missing section.
    /// </summary>
    private IReadOnlyList<(string Label, IReadOnlyList<ShowcaseApiMember> Rows)> GeneratedGroups
    {
        get
        {
            if (GeneratedMembers.Count == 0 && GeneratedContracts.Count == 0)
            {
                return [];
            }

            return new (string Label, IReadOnlyList<ShowcaseApiMember> Rows)[]
            {
                ("属性", [.. BaseMembers, .. GeneratedMembers.Where(row => row.Kind == "属性")]),
                ("事件回调", [.. GeneratedMembers.Where(row => row.Kind == "事件回调")]),
                ("接口 / 服务", GeneratedContracts)
            }.Where(group => group.Rows.Count > 0).ToArray();
        }
    }

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
