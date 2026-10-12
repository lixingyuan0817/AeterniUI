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
        ShowcaseApi.CommonParameters.Concat(Parameters ?? []).ToArray();

    /// <summary>
    /// Whether this page documents an API at all. The generated table lives in
    /// <c>ShowcaseApiTable</c> so the page component and the demo-card page share one
    /// renderer; this flag only decides whether the section (and its step number) exists.
    /// </summary>
    private bool HasApiSection =>
        ShowcaseApiReference.ByPage.ContainsKey(AnchorId)
        || ShowcaseApiReference.Contracts.ContainsKey(AnchorId)
        || Parameters is not null
        || Events is not null
        || Contracts is not null;

    private bool HasGeneratedApi =>
        ShowcaseApiReference.ByPage.ContainsKey(AnchorId) || ShowcaseApiReference.Contracts.ContainsKey(AnchorId);
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
