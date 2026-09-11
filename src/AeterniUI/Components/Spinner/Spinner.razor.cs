using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Spinner;

/// <summary>
/// Standalone loading indicator. The visual definition is shared with
/// <c>Button</c>'s loading veil, which only overrides the documented
/// <c>--aeterni-spinner-size</c> variable to match its own icon slot.
/// </summary>
public partial class Spinner : AeterniComponent
{
    /// <summary>Ring size tier.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    /// <summary>
    /// Semantic colour. <see cref="Color.Default" /> inherits the surrounding text
    /// colour, which is what the button loading veil relies on.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Default;

    /// <summary>
    /// Accessible name. Falls back to
    /// <see cref="Services.AeterniUITextOptions.SpinnerLabel" />.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.SpinnerLabel : AriaLabel.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown spinner size.");
        }

        if (!Enum.IsDefined(Color))
        {
            throw new ArgumentOutOfRangeException(nameof(Color), Color, "Unknown spinner color.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-spinner")
        .Add(ComponentClass.ForSize("aeterni-spinner", Size))
        .Add(ComponentClass.ForColor("aeterni-spinner", Color));

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        // A bare ring carries no text, so the busy state has to be announced
        // through a status role plus an accessible name.
        return new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "status",
            ["aria-label"] = EffectiveLabel
        };
    }
}
