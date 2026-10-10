using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.FlashCardGroup;

/// <summary>
/// A small deck of <see cref="FlashCard"/> components. Cards rest in a compact
/// stack and fan out when the group is hovered or one of its cards receives focus.
/// </summary>
public partial class FlashCardGroup : AeterniComponent
{
    /// <summary>Cards to render in the deck, normally direct <see cref="FlashCard"/> children.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Accessible name for the card collection. Empty falls back to
    /// <c>AeterniUITextOptions.FlashCardGroupLabel</c>, so a host can localise it
    /// through the text table instead of having to pass the name on every deck.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.FlashCardGroupLabel : AriaLabel.Trim();

    /// <summary>Whether the deck is kept open without pointer hover.</summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>Whether pointer hover and focus-within fan the cards out.</summary>
    [Parameter]
    public bool HoverExpand { get; set; } = true;

    /// <summary>Width of each card in pixels.</summary>
    [Parameter]
    public double CardWidth { get; set; } = 220;

    /// <summary>Horizontal distance between cards in the expanded fan, in pixels.</summary>
    [Parameter]
    public double ExpandedSpacing { get; set; } = 72;

    /// <summary>Small vertical offset between cards while they are stacked, in pixels.</summary>
    [Parameter]
    public double CollapsedOffset { get; set; } = 5;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ValidateLength(CardWidth, nameof(CardWidth), allowZero: false);
        ValidateLength(ExpandedSpacing, nameof(ExpandedSpacing), allowZero: true);
        ValidateLength(CollapsedOffset, nameof(CollapsedOffset), allowZero: true);
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-flash-card-group")
        .Add("is-expanded", Expanded)
        .Add("is-hover-expand", HoverExpand)
        .Add("is-disabled", Disabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "group",
            ["aria-label"] = EffectiveAriaLabel
        };

        if (Disabled)
        {
            attributes["aria-disabled"] = "true";
            attributes["inert"] = true;
        }

        return attributes;
    }

    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-flash-card-group-card-width", $"{CardWidth.ToString(CultureInfo.InvariantCulture)}px")
        .Add("--aeterni-flash-card-group-expanded-spacing", $"{ExpandedSpacing.ToString(CultureInfo.InvariantCulture)}px")
        .Add("--aeterni-flash-card-group-collapsed-offset", $"{CollapsedOffset.ToString(CultureInfo.InvariantCulture)}px");

    private static void ValidateLength(double value, string parameterName, bool allowZero)
    {
        if (!double.IsFinite(value) || (allowZero ? value < 0 : value <= 0))
        {
            var requirement = allowZero ? "non-negative" : "positive";
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be a {requirement} finite number.");
        }
    }
}
