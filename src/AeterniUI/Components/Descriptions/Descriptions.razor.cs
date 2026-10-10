using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Descriptions;

/// <summary>A single label/value pair displayed by <see cref="Descriptions"/>.</summary>
/// <param name="Label">Visible field label.</param>
/// <param name="Content">Plain-text field value used by the default presentation.</param>
/// <param name="Span">Number of layout columns occupied by this item.</param>
/// <param name="Visible">Whether the item is rendered.</param>
public sealed record DescriptionItem(
    string Label,
    string? Content = null,
    int Span = 1,
    bool Visible = true);

/// <summary>
/// Displays host-provided label/value pairs with native description-list semantics.
/// The component is presentational: ordering, filtering and rich content remain
/// owned by the host.
/// </summary>
public partial class Descriptions : AeterniComponent
{
    /// <summary>Items rendered in the supplied order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DescriptionItem> Items { get; set; } = [];

    /// <summary>Replaces the default plain-text value for each item.</summary>
    [Parameter]
    public RenderFragment<DescriptionItem>? ItemTemplate { get; set; }

    /// <summary>Horizontal keeps labels beside values; vertical stacks each pair.</summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>Number of layout columns available to item spans.</summary>
    [Parameter]
    public int Columns { get; set; } = 3;

    /// <summary>Draws a neutral border around the description grid and its items.</summary>
    [Parameter]
    public bool Bordered { get; set; }

    /// <summary>Optional accessible name for the description list.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private IReadOnlyList<DescriptionItem> VisibleItems => Items.Where(item => item.Visible).ToArray();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Items is null)
        {
            throw new ArgumentNullException(nameof(Items));
        }

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown descriptions orientation.");
        }

        if (Columns is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(Columns), Columns, "Descriptions columns must be between 1 and 4.");
        }

        if (Items.Any(item => item is null))
        {
            throw new ArgumentException("Description items cannot be null.", nameof(Items));
        }

        foreach (var item in Items)
        {
            if (string.IsNullOrWhiteSpace(item.Label))
            {
                throw new ArgumentException("Description item labels cannot be empty.", nameof(Items));
            }

            if (item.Span is < 1 || item.Span > Columns)
            {
                throw new ArgumentException($"Description item '{item.Label}' span must be between 1 and {Columns}.", nameof(Items));
            }
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-descriptions")
        .Add("aeterni-descriptions--vertical", Orientation == Orientation.Vertical)
        .Add("aeterni-descriptions--bordered", Bordered);

    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-descriptions-columns", Columns);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel.Trim();
        }

        return attributes;
    }

    private static string GetItemStyle(DescriptionItem item) =>
        $"grid-column: span {item.Span};";
}
