using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Breadcrumb;

/// <summary>A single level in a <see cref="Breadcrumb"/> navigation trail.</summary>
/// <param name="Label">Visible text for this level.</param>
/// <param name="Href">Optional destination. The component does not perform routing.</param>
/// <param name="Icon">Optional decorative icon rendered before the label.</param>
/// <param name="Disabled">Whether this level is presented as unavailable.</param>
/// <param name="Visible">Whether this level is rendered.</param>
public sealed record BreadcrumbItem(
    string Label,
    string? Href = null,
    RenderFragment? Icon = null,
    bool Disabled = false,
    bool Visible = true);

/// <summary>
/// Renders a hierarchical navigation trail. Links are deliberately left to the
/// browser and host application; the component does not know about routing.
/// </summary>
public partial class Breadcrumb : AeterniComponent
{
    [Parameter, EditorRequired]
    public IReadOnlyList<BreadcrumbItem> Items { get; set; } = [];

    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Optional separator content. Defaults to a decorative chevron.</summary>
    [Parameter]
    public RenderFragment? Separator { get; set; }

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.BreadcrumbLabel
        : AriaLabel.Trim();

    private IReadOnlyList<BreadcrumbItem> VisibleItems => Items.Where(item => item.Visible).ToArray();

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-breadcrumb");

    internal bool IsCurrent(int index) => index == VisibleItems.Count - 1;

}
