namespace AeterniUI.Enums;

/// <summary>
/// Icon slot size of <c>NavMenu</c> rows. The slot also forwards its size through
/// <c>--aeterni-icon-render-size</c>, so an icon in a row template follows the
/// tier without the host passing an explicit size.
/// </summary>
public enum NavMenuIconSize
{
    /// <summary>
    /// Default tier: a 16px slot, the existing <c>--aeterni-icon-size-md</c> value.
    /// </summary>
    Default,

    /// <summary>
    /// Small tier: a 14px slot (<c>--aeterni-icon-size-sm</c>).
    /// </summary>
    Small,

    /// <summary>
    /// Large tier: a 20px slot (<c>--aeterni-icon-size-lg</c>).
    /// </summary>
    Large
}
