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
    /// Variant modifier for a surface-like container, or <see langword="null" />
    /// for the base style.
    /// </summary>
    /// <remarks>
    /// Card and Surface render the same surface recipe under different root
    /// classes and cannot share a stylesheet (CSS isolation keeps each
    /// component's rules to itself), so the option-to-modifier mapping lives here
    /// instead of being written out twice and left to drift.
    /// </remarks>
    public static string? ForSurfaceVariant(SurfaceVariant variant) => variant switch
    {
        SurfaceVariant.Subtle => "subtle",
        SurfaceVariant.Elevated => "elevated",
        SurfaceVariant.Glass => "glass",
        _ => null
    };

    /// <summary>Elevation modifier for a surface-like container.</summary>
    public static string? ForSurfaceElevation(SurfaceElevation elevation) => elevation switch
    {
        SurfaceElevation.Small => "elevation-small",
        SurfaceElevation.Medium => "elevation-medium",
        SurfaceElevation.Large => "elevation-large",
        _ => null
    };

    /// <summary>
    /// Padding modifier for a surface-like container. Unlike the other surface
    /// options the default tier does carry a modifier, because the two components
    /// differ in what "unset" means to them.
    /// </summary>
    public static string? ForSurfacePadding(SurfacePadding padding) => padding switch
    {
        SurfacePadding.None => "padding-none",
        SurfacePadding.Small => "padding-small",
        SurfacePadding.Large => "padding-large",
        SurfacePadding.ExtraLarge => "padding-extra-large",
        _ => "padding-medium"
    };

    /// <summary>Radius modifier for a surface-like container.</summary>
    public static string? ForSurfaceRadius(SurfaceRadius radius) => radius switch
    {
        SurfaceRadius.None => "radius-none",
        SurfaceRadius.Small => "radius-small",
        SurfaceRadius.Medium => "radius-medium",
        SurfaceRadius.Large => "radius-large",
        SurfaceRadius.ExtraLarge => "radius-extra-large",
        SurfaceRadius.Round => "radius-round",
        _ => null
    };

    /// <summary>
    /// Native <c>type</c> attribute value for a button.
    /// </summary>
    /// <remarks>
    /// The fallback is deliberately <c>button</c> rather than an exception: an
    /// unknown value must never degrade to the native default, because the HTML
    /// default for an invalid <c>type</c> is <c>submit</c> and would make the
    /// control submit its surrounding form. Components still validate the
    /// parameter up front so callers get an exception instead of a silent change
    /// of behaviour; this mapping is the safety net behind that check.
    /// </remarks>
    public static string ForButtonType(ButtonType type) => type switch
    {
        ButtonType.Submit => "submit",
        ButtonType.Reset => "reset",
        _ => "button"
    };

    /// <summary>
    /// Maps notification severities onto the shared semantic colour used by
    /// buttons, progress bars and icons. This is the only Severity→Color mapping
    /// in the library.
    /// </summary>
    public static ButtonIntent ToButtonIntent(this Severity severity) => severity switch
    {
        Severity.Warning => ButtonIntent.Warning,
        Severity.Danger => ButtonIntent.Danger,
        _ => ButtonIntent.Default
    };

    /// <summary>
    /// Maps notification severities onto the shared semantic colour used by
    /// notification cards, progress bars and icons.
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
