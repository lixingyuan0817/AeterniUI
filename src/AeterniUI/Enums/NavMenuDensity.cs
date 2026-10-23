namespace AeterniUI.Enums;

/// <summary>
/// Row density of <c>NavMenu</c>. Only the row box retunes: hover, selected and
/// pressed paint stay on the same row element, so every tier keeps the same
/// state model.
/// </summary>
public enum NavMenuDensity
{
    /// <summary>
    /// Default tier, identical to the pre-density look: 32px minimum row height
    /// (<c>--aeterni-row-height-compact</c>), 0 8px row padding, 6px row radius,
    /// 4px list gap, 14px label and <c>--aeterni-text-secondary</c> row ink.
    /// </summary>
    Default,

    /// <summary>
    /// Compact tier: 28px minimum row height (<c>--aeterni-control-height-sm</c>),
    /// 0 8px row padding, 6px row radius, 4px list gap, 12px label and
    /// <c>--aeterni-text-secondary</c> row ink.
    /// </summary>
    Compact,

    /// <summary>
    /// Comfortable tier aligned with the workbench sidebar: 38px minimum row
    /// height (<c>calc(36px control height + 2 × 1px border)</c>), 3px block plus
    /// 8px inline row padding, 8px row radius (<c>--aeterni-radius-lg</c>), 2px
    /// list gap, 13px label and <c>--aeterni-text-primary</c> row ink.
    /// </summary>
    Comfortable
}
