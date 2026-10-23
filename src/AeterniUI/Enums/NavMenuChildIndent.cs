namespace AeterniUI.Enums;

/// <summary>
/// Indent of a nested <c>NavMenu</c> level: the margin before the guide line and
/// the padding between that line and the nested row content.
/// </summary>
public enum NavMenuChildIndent
{
    /// <summary>
    /// Default tier: a 16px guide-line margin plus 8px content padding, the
    /// existing <c>--aeterni-spacing-4</c> / <c>--aeterni-spacing-2</c> pair.
    /// </summary>
    Default,

    /// <summary>
    /// Compact tier: a 12px guide-line margin plus 4px content padding
    /// (<c>--aeterni-spacing-3</c> / <c>--aeterni-spacing-1</c>).
    /// </summary>
    Compact,

    /// <summary>
    /// Spacious tier: a 20px guide-line margin plus 12px content padding
    /// (<c>--aeterni-spacing-5</c> / <c>--aeterni-spacing-3</c>).
    /// </summary>
    Spacious
}
