using AeterniUI.Enums;

namespace AeterniUI.Components;

/// <summary>
/// Central mapping from public component options to their CSS modifier classes.
/// Every component uses these helpers, so the "default tier emits no modifier"
/// convention and the semantic colour names live in exactly one place.
/// </summary>
internal static class ComponentClass
{
    /// <summary>
    /// Size modifier class for a component's root class, or <see langword="null" />
    /// when the size needs no modifier.
    /// </summary>
    /// <param name="prefix">Root class of the component, e.g. <c>aeterni-button</c>.</param>
    /// <param name="size">Requested size tier.</param>
    /// <param name="mediumModifier">
    /// Modifier emitted for <see cref="Size.Medium" /> when a component styles that
    /// tier explicitly (only <c>Icon</c> does). Otherwise <see cref="Size.Default" />
    /// and <see cref="Size.Medium" /> share the same unmodified tier.
    /// </param>
    public static string? ForSize(string prefix, Size size, string? mediumModifier = null) => size switch
    {
        Size.Small => $"{prefix}--sm",
        Size.Large => $"{prefix}--lg",
        Size.Medium when mediumModifier is not null => $"{prefix}--{mediumModifier}",
        _ => null
    };

    /// <summary>
    /// Modifier class for a component option, or <see langword="null" /> when the
    /// option means "use the component's base style".
    /// </summary>
    public static string? For(string prefix, string? modifier) =>
        string.IsNullOrWhiteSpace(modifier) ? null : $"{prefix}--{modifier}";

    /// <summary>
    /// Semantic colour modifier class, or <see langword="null" /> for
    /// <see cref="Color.Default" /> (the component's base style).
    /// </summary>
    public static string? ForColor(string prefix, Color color) => color switch
    {
        Color.Primary => $"{prefix}--primary",
        Color.Neutral => $"{prefix}--neutral",
        Color.Success => $"{prefix}--success",
        Color.Warning => $"{prefix}--warning",
        Color.Danger => $"{prefix}--danger",
        Color.Info => $"{prefix}--info",
        _ => null
    };

    /// <summary>
    /// Maps notification severities onto the shared semantic colour used by
    /// buttons, progress bars and icons. This is the only Severity→Color mapping
    /// in the library.
    /// </summary>
    public static Color ToColor(this Severity severity) => severity switch
    {
        Severity.Success => Color.Success,
        Severity.Warning => Color.Warning,
        Severity.Danger => Color.Danger,
        Severity.Info => Color.Info,
        _ => Color.Primary
    };
}
