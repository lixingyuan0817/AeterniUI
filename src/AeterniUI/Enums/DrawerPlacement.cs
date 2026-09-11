namespace AeterniUI.Enums;

/// <summary>
/// Viewport edge a <c>Drawer</c> slides in from. <see cref="Start" /> and
/// <see cref="End" /> follow the writing direction, so they mirror in RTL.
/// </summary>
public enum DrawerPlacement
{
    /// <summary>Inline start edge (left in LTR, right in RTL).</summary>
    Start,

    /// <summary>Inline end edge (right in LTR, left in RTL).</summary>
    End,

    Top,

    Bottom
}
