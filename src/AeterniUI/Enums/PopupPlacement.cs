namespace AeterniUI.Enums;

/// <summary>
/// Where a <c>Popover</c> layer is anchored relative to its <c>PopupHost</c>.
/// The layer flips to the opposite side when the preferred side does not fit in
/// the viewport and is shifted along the cross axis to stay inside it.
/// </summary>
public enum PopupPlacement
{
    /// <summary>Below the host, aligned to its inline start.</summary>
    BottomStart,

    /// <summary>Below the host, aligned to its inline end.</summary>
    BottomEnd,

    /// <summary>Above the host, aligned to its inline start.</summary>
    TopStart,

    /// <summary>Above the host, aligned to its inline end.</summary>
    TopEnd
}
