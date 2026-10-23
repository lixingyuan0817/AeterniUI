namespace AeterniUI.Enums;

/// <summary>
/// Ink tier of an icon.
/// </summary>
/// <remarks>
/// <see cref="Color" /> carries semantic colour (brand, success, danger…);
/// <see cref="IconTone" /> carries the neutral ink ladder (secondary, tertiary).
/// The two are separate axes: a semantic colour stays semantic, a tone only moves
/// the icon down the text ink scale.
/// </remarks>
public enum IconTone
{
    /// <summary>Inherits <c>currentColor</c> from the surrounding ink; the default.</summary>
    Default,

    /// <summary>Secondary ink, for supporting glyphs next to primary text.</summary>
    Muted,

    /// <summary>Tertiary ink, for the quietest chrome glyphs in dense surfaces.</summary>
    Tertiary
}
