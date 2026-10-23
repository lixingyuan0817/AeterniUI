namespace AeterniUI.Enums;

/// <summary>
/// Size of the optional description line under a <c>NavMenu</c> row label. The
/// label itself always follows the density tier's font size; only the secondary
/// description line is retuned here.
/// </summary>
public enum NavMenuTextSize
{
    /// <summary>
    /// Default tier: 11px, the existing
    /// <c>calc(--aeterni-font-size-xs - 1px)</c> value. Kept as the default so the
    /// pre-tier look is unchanged.
    /// </summary>
    Default,

    /// <summary>
    /// Small tier: 10px (<c>calc(--aeterni-font-size-xs - 2px)</c>).
    /// </summary>
    Small,

    /// <summary>
    /// Large tier: 12px (<c>--aeterni-font-size-xs</c>).
    /// </summary>
    Large
}
