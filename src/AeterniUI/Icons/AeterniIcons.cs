namespace AeterniUI.Icons;

/// <summary>
/// Built-in icon definitions owned by AeterniUI and used by the components
/// themselves. The geometry is authored on a 16x16 grid so that components keep
/// stable sizes without depending on an icon vendor package. Consumers can reuse
/// these definitions or plug in an adapter such as <c>AeterniUI.Icons.FontAwesome</c>.
/// </summary>
public static class AeterniIcons
{
    /// <summary>Check mark used by Checkbox and success notices.</summary>
    public static IconDefinition Check { get; } = new(
        "check",
        16,
        16,
        "M2.9 8.4 6.4 11.7 13.1 4.5 12.2 3.7 6.5 9.8 3.8 7.3z");

    /// <summary>Downward chevron used by ComboBox and collapsible groups.</summary>
    public static IconDefinition ChevronDown { get; } = new(
        "chevron-down",
        16,
        16,
        "M4.2 6 8 9.8 11.8 6l.9.9L8 11.6 3.3 6.9z");

    /// <summary>Rightward chevron used by collapsed groups (rotated to point down when open).</summary>
    public static IconDefinition ChevronRight { get; } = new(
        "chevron-right",
        16,
        16,
        "M6 11.8 9.8 8 6 4.2l.9-.9L11.6 8 6.9 12.7z");

    /// <summary>Dismiss cross used by Tag, Dialog close buttons and danger notices.</summary>
    public static IconDefinition Xmark { get; } = new(
        "xmark",
        16,
        16,
        [
            "M4.21 2.79 13.21 11.79 11.79 13.21 2.79 4.21z",
            "M13.21 4.21 4.21 13.21 2.79 11.79 11.79 2.79z"
        ]);

    /// <summary>Solid star used by Rating when no custom icon is supplied.</summary>
    public static IconDefinition Star { get; } = new(
        "star",
        16,
        16,
        "M8 1.6 9.6 5.8 14.09 6.02 10.59 8.84 11.76 13.18 8 10.72 4.24 13.18 5.41 8.84 1.91 6.02 6.4 5.8z");

    /// <summary>Open box used as the default placeholder of the empty state.</summary>
    public static IconDefinition EmptyBox { get; } = new(
        "empty-box",
        16,
        16,
        [
            "M2.5 3.1 h4.2 l1.3 1.6 h5.5 v1.7 H2.5z",
            "M2.5 6.3 h1.7 v5.5 h7.6 V6.3 h1.7 v7.2 H2.5z"
        ]);

    /// <summary>Information mark used by informational notices.</summary>
    public static IconDefinition Info { get; } = new(
        "info",
        16,
        16,
        [
            "M8 3.1a1.2 1.2 0 1 0 0 2.4a1.2 1.2 0 1 0 0 -2.4z",
            "M7.1 6.75 8.9 6.75 8.9 12.5 7.1 12.5z"
        ]);

    /// <summary>Exclamation mark used by warning notices.</summary>
    public static IconDefinition Exclamation { get; } = new(
        "exclamation",
        16,
        16,
        [
            "M7.1 3.5 8.9 3.5 8.9 9.6 7.1 9.6z",
            "M8 10.75a1.2 1.2 0 1 0 0 2.4a1.2 1.2 0 1 0 0 -2.4z"
        ]);
}
