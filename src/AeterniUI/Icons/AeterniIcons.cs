namespace AeterniUI.Icons;

/// <summary>
/// Built-in icon definitions owned by AeterniUI and used by the components
/// themselves. The geometry is authored on a 16x16 grid so that components keep
/// stable sizes without depending on an icon vendor package. Consumers can reuse
/// these definitions or plug in an adapter such as <c>AeterniUI.Icons.FontAwesome</c>.
/// </summary>
public static class AeterniIcons
{
    /// <summary>Calendar glyph used by date selection controls.</summary>
    public static IconDefinition Calendar { get; } = new(
        "calendar",
        16,
        16,
        [
            "M3 3.8h1.5v-1.2h1.2v1.2h4.6v-1.2h1.2v1.2H13a1 1 0 0 1 1 1v8.2a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V4.8a1 1 0 0 1 1-1zM3.3 6v6.7h9.4V6z",
            "M3 5.1h10v1H3z",
            "M4.2 7.4h1.4v1.4H4.2zm3.1 0h1.4v1.4H7.3zm3.1 0h1.4v1.4h-1.4zM4.2 10h1.4v1.4H4.2zm3.1 0h1.4v1.4H7.3zm3.1 0h1.4v1.4h-1.4z"
        ]);

    /// <summary>Clock glyph used by time selection controls.</summary>
    public static IconDefinition Clock { get; } = new(
        "clock",
        16,
        16,
        [
            "M8 2a6 6 0 1 1 0 12A6 6 0 0 1 8 2zm0 1.3a4.7 4.7 0 1 0 0 9.4 4.7 4.7 0 0 0 0-9.4z",
            "M7.4 4.5h1.2v3.2l2.5 1.5-.6 1-3.1-1.8z"
        ]);

    /// <summary>Check mark used by Checkbox and success notices.</summary>
    public static IconDefinition Check { get; } = new(
        "check",
        16,
        16,
        "M2.8 8.2 5.9 11.3 13.2 4.1 12.4 3.3 5.9 9.8 3.6 7.5z");

    /// <summary>Downward chevron used by ComboBox and collapsible groups.</summary>
    public static IconDefinition ChevronDown { get; } = new(
        "chevron-down",
        16,
        16,
        "M4.1 6.2 8 10.1 11.9 6.2 12.8 7.1 8 11.9 3.2 7.1z");

    /// <summary>Rightward chevron used by collapsed groups (rotated to point down when open).</summary>
    public static IconDefinition ChevronRight { get; } = new(
        "chevron-right",
        16,
        16,
        "M6 4.1 9.8 8 6 11.9 5.1 11 7.7 8 5.1 5z");

    /// <summary>Leftward chevron used by calendar navigation.</summary>
    public static IconDefinition ChevronLeft { get; } = new(
        "chevron-left",
        16,
        16,
        "M10 4.1 6.2 8 10 11.9 10.9 11 8.3 8 10.9 5z");

    /// <summary>Dismiss cross used by Tag, Dialog close buttons and danger notices.</summary>
    public static IconDefinition Xmark { get; } = new(
        "xmark",
        16,
        16,
        [
            "M4.2 2.9 8 6.7 11.8 2.9 13 4.1 9.2 7.9 13 11.7 11.8 12.9 8 9.1 4.2 12.9 3 11.7 6.8 7.9 3 4.1z"
        ]);

    /// <summary>Solid star used by Rating when no custom icon is supplied.</summary>
    public static IconDefinition Star { get; } = new(
        "star",
        16,
        16,
        "M8 1.6 9.7 5.6 14 5.9 10.7 8.6 11.8 12.8 8 10.8 4.2 12.8 5.3 8.6 2 5.9 6.3 5.6z");

    /// <summary>Open box used as the default placeholder of the empty state.</summary>
    public static IconDefinition EmptyBox { get; } = new(
        "empty-box",
        16,
        16,
        [
            "M2.6 3.6h3.9l1.5 1.7h5.4v1.8H2.6z",
            "M2.6 6.8h10.8v5.8H2.6z",
            "M4.5 8h7v1.6h-7z"
        ]);

    /// <summary>Information mark used by informational notices.</summary>
    public static IconDefinition Info { get; } = new(
        "info",
        16,
        16,
        [
            "M8 2.8a5.2 5.2 0 1 1 0 10.4A5.2 5.2 0 0 1 8 2.8zm0 2.9a1.2 1.2 0 1 0 0 2.4 1.2 1.2 0 0 0 0-2.4zm-1 4.4h2.1v3.6H7z"
        ]);

    /// <summary>Exclamation mark used by warning notices.</summary>
    public static IconDefinition Exclamation { get; } = new(
        "exclamation",
        16,
        16,
        [
            "M8 2.2 13.4 12.8H2.6zm0 3.8a1.2 1.2 0 1 0 0 2.4 1.2 1.2 0 0 0 0-2.4zm-1.1 4.3h2.2v1.8H6.9z"
        ]);
}
