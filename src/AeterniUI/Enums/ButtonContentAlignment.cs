namespace AeterniUI.Enums;

/// <summary>
/// Horizontal placement of a <c>Button</c>'s content row.
/// </summary>
/// <remarks>
/// Only the arrangement inside the button changes. The button's own alignment and
/// width contract stay as they are, so <c>FullWidth</c> is still what makes the
/// control fill its container.
/// </remarks>
public enum ButtonContentAlignment
{
    /// <summary>Content stays centred; the default.</summary>
    Center,

    /// <summary>
    /// Content starts at the leading edge and fills the available width, for a
    /// full-width row action with an icon and a label.
    /// </summary>
    Start
}
