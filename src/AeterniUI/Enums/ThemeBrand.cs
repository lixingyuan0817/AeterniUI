namespace AeterniUI.Enums;

/// <summary>
/// Brand hue layer. Orthogonal to <see cref="ThemeMode"/>: the brand picks which
/// palette is in effect, the mode picks which stops of that palette apply.
/// Each value maps to a <c>[data-aeterni-brand="…"]</c> block in the stylesheet,
/// so switching re-tints fills, accent text, links, focus rings and interaction
/// states without any component opting in.
/// </summary>
public enum ThemeBrand
{
    /// <summary>Violet brand. The library default.</summary>
    Purple,

    /// <summary>Green brand.</summary>
    Green,

    /// <summary>Burnt-orange brand.</summary>
    Orange
}
