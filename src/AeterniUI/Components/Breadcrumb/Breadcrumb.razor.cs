using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Breadcrumb;

/// <summary>A single level in a <see cref="Breadcrumb"/> navigation trail.</summary>
/// <param name="Label">Visible text for this level.</param>
/// <param name="Href">Optional destination. The component does not perform routing.</param>
/// <param name="Icon">Optional decorative icon rendered before the label.</param>
/// <param name="Disabled">Whether this level is presented as unavailable.</param>
/// <param name="Visible">Whether this level is rendered.</param>
/// <param name="Target">Optional browsing context, matching <c>MenuItem.Target</c>.</param>
public sealed record BreadcrumbItem(
    string Label,
    string? Href = null,
    RenderFragment? Icon = null,
    bool Disabled = false,
    bool Visible = true,
    string? Target = null);

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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Items is null)
        {
            throw new ArgumentNullException(nameof(Items));
        }

        if (Items.Any(item => item is null))
        {
            throw new ArgumentException("Breadcrumb items cannot be null.", nameof(Items));
        }

        if (Items.Any(item => string.IsNullOrWhiteSpace(item.Label)))
        {
            throw new ArgumentException("Breadcrumb item labels cannot be empty.", nameof(Items));
        }
    }

    private IReadOnlyList<BreadcrumbItem> VisibleItems => Items.Where(item => item.Visible).ToArray();

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-breadcrumb");

    internal static bool IsCurrent(int index, int visibleCount) => index == visibleCount - 1;

    internal static string? LinkRel(BreadcrumbItem item) =>
        string.Equals(item.Target, "_blank", StringComparison.OrdinalIgnoreCase) ? "noopener noreferrer" : null;
}
