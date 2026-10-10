using System.Globalization;
using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Timeline;

/// <summary>A single event displayed by <see cref="Timeline"/>.</summary>
/// <param name="Title">Visible event heading used by the default presentation.</param>
/// <param name="Description">Optional supporting text.</param>
/// <param name="Timestamp">Optional machine-readable instant.</param>
/// <param name="TimeText">
/// Optional visible time label. When omitted, <paramref name="Timestamp"/> is
/// formatted with the current culture.
/// </param>
/// <param name="Icon">Optional marker glyph; otherwise a dot is rendered.</param>
/// <param name="Color">Semantic marker colour. The default is the brand colour.</param>
/// <param name="Visible">Whether the event is rendered.</param>
public sealed record TimelineItem(
    string Title,
    string? Description = null,
    DateTimeOffset? Timestamp = null,
    string? TimeText = null,
    IconDefinition? Icon = null,
    Color Color = Color.Default,
    bool Visible = true);

/// <summary>
/// Displays host-provided events in chronological order. Timeline is purely
/// presentational: the host owns ordering, filtering and any business state.
/// </summary>
public partial class Timeline : AeterniComponent
{
    [Parameter, EditorRequired]
    public IReadOnlyList<TimelineItem> Items { get; set; } = [];

    /// <summary>
    /// Replaces the default title and description for each event. The marker and
    /// time remain component-owned so custom content keeps the timeline rhythm.
    /// </summary>
    [Parameter]
    public RenderFragment<TimelineItem>? ItemTemplate { get; set; }

    /// <summary>Optional accessible name for the ordered list.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Direction of the event axis. The default is vertical.</summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    private IReadOnlyList<TimelineItem> VisibleItems => Items.Where(item => item.Visible).ToArray();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Items is null)
        {
            throw new ArgumentNullException(nameof(Items));
        }

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown timeline orientation.");
        }

        if (Items.Any(item => item is null))
        {
            throw new ArgumentException("Timeline items cannot be null.", nameof(Items));
        }

        foreach (var item in Items)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                throw new ArgumentException("Timeline item titles cannot be empty.", nameof(Items));
            }

            if (!Enum.IsDefined(item.Color))
            {
                throw new ArgumentException($"Timeline item '{item.Title}' has an unknown color.", nameof(Items));
            }
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-timeline")
        .Add("aeterni-timeline--horizontal", Orientation == Orientation.Horizontal);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel.Trim();
        }

        return attributes;
    }

    private static string GetItemClass(TimelineItem item) => new ClassBuilder("aeterni-timeline__item")
        .Add(ComponentClass.ForColor("aeterni-timeline__item", item.Color))
        .Build();

    private static string? GetTimeText(TimelineItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.TimeText))
        {
            return item.TimeText.Trim();
        }

        return item.Timestamp?.ToString("g", CultureInfo.CurrentCulture);
    }

    private static string GetMachineTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToString("O", CultureInfo.InvariantCulture);
}
