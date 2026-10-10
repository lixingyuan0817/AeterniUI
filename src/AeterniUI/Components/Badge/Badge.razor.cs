using System.Globalization;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Badge;

/// <summary>
/// Count or dot indicator attached to the top-inline-end corner of a single child
/// element (icon, avatar or button).
/// </summary>
public partial class Badge : AeterniComponent
{
    /// <summary>
    /// Number to display. <see langword="null" /> renders no indicator unless
    /// <see cref="Dot" /> is set; an explicit 0 is displayed as 0.
    /// </summary>
    [Parameter]
    public int? Count { get; set; }

    /// <summary>Highest count shown in full; larger values render as <c>{Max}+</c>.</summary>
    [Parameter]
    public int Max { get; set; } = 99;

    /// <summary>Dot mode: renders a small dot and ignores <see cref="Count" />.</summary>
    [Parameter]
    public bool Dot { get; set; }

    /// <summary>Semantic colour of the indicator. <see cref="Color.Default" /> is the brand fill.</summary>
    [Parameter]
    public Color Color { get; set; } = Color.Default;

    /// <summary>
    /// Accessible name. In count mode it replaces the bare number; in dot mode it
    /// replaces <see cref="Services.AeterniUITextOptions.BadgeLabel" />.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>The element the indicator is attached to.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool HasIndicator => Dot || Count.HasValue;

    private string? CountText => Count is { } count
        ? count > Max
            ? $"{Max.ToString(CultureInfo.InvariantCulture)}+"
            : count.ToString(CultureInfo.InvariantCulture)
        : null;

    private string DotLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.BadgeLabel : AriaLabel.Trim();

    private string? IndicatorAriaLabel =>
        Dot || string.IsNullOrWhiteSpace(AriaLabel) ? null : AriaLabel.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Color))
        {
            throw new ArgumentOutOfRangeException(nameof(Color), Color, "Unknown badge color.");
        }

        if (Max < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Max), Max, "The badge maximum must be at least 1.");
        }

        if (Count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Count), Count, "A badge count cannot be negative.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-badge")
        .Add(ComponentClass.ForColor("aeterni-badge", Color))
        .Add("is-dot", Dot);
}
