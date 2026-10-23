using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Icon;

public partial class Icon : AeterniComponent
{
    [Parameter, EditorRequired]
    public IconDefinition Definition { get; set; } = null!;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public Color Color { get; set; } = Color.Default;

    /// <summary>
    /// Ink tier of the icon.
    /// </summary>
    /// <remarks>
    /// <see cref="Color" /> carries semantic colour and <see cref="Tone" /> carries
    /// the neutral ink ladder: a semantic colour stays semantic, a tone moves the
    /// glyph down the text ink scale (secondary / tertiary). The default value
    /// keeps <c>currentColor</c> so the icon follows its surrounding ink.
    /// </remarks>
    [Parameter]
    public IconTone Tone { get; set; } = IconTone.Default;

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? Title { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown icon size.");
        }

        if (!Enum.IsDefined(Color))
        {
            throw new ArgumentOutOfRangeException(nameof(Color), Color, "Unknown icon color.");
        }

        if (!Enum.IsDefined(Tone))
        {
            throw new ArgumentOutOfRangeException(nameof(Tone), Tone, "Unknown icon tone.");
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-icon")
            .Add(SizeClass)
            .Add(ColorClass)
            .Add(ToneClass);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        ArgumentNullException.ThrowIfNull(Definition);

        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            ["viewBox"] = Definition.ViewBox,
            ["fill"] = "currentColor",
            ["xmlns"] = "http://www.w3.org/2000/svg",
            ["focusable"] = "false"
        };

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["role"] = "img";
            attributes["aria-label"] = AriaLabel.Trim();
        }
        else
        {
            attributes["aria-hidden"] = "true";
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            attributes["title"] = Title.Trim();
        }

        return attributes;
    }

    // Icon is the one component whose middle tier has an explicit rule (a fixed
    // 16px glyph box) instead of inheriting the parent icon slot.
    private string? SizeClass => ComponentClass.ForSize("aeterni-icon", Size, "md");

    // Color.Default keeps `currentColor`, so the modifier comes from the shared
    // option→class mapping instead of a second colour switch. The stylesheet only
    // implements the six named colours.
    private string? ColorClass => ComponentClass.ForColor("aeterni-icon", Color);

    // Tone is the ink ladder next to the semantic Colour axis. Both are
    // single-class modifiers, so the stylesheet declares the tone rules after the
    // colour rules and a tone wins when a caller sets both.
    private string? ToneClass => Tone switch
    {
        IconTone.Muted => "aeterni-icon--tone-muted",
        IconTone.Tertiary => "aeterni-icon--tone-tertiary",
        _ => null
    };
}
