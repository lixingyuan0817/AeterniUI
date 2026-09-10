namespace AeterniUI.Services;

/// <summary>
/// User-visible text that AeterniUI renders on its own. Every entry has an
/// English default; override any of them through
/// <see cref="AeterniServiceCollectionExtensions.AddAeterniUI" /> to localise the
/// library. Components fall back to these values only when the matching
/// parameter was not supplied, so per-instance overrides keep working.
/// </summary>
public sealed class AeterniUITextOptions
{
    /// <summary>Trigger text of a <c>ComboBox</c> that has no value yet.</summary>
    public string ComboBoxPlaceholder { get; set; } = "Select";

    /// <summary>Accessible name of the <c>ComboBox</c> option list.</summary>
    public string ComboBoxListLabel { get; set; } = "Options";

    /// <summary>Accessible name of a <c>Rating</c> group.</summary>
    public string RatingLabel { get; set; } = "Rating";

    /// <summary>Accessible name of the <c>ThemeSwitch</c> group.</summary>
    public string ThemeSwitchLabel { get; set; } = "Theme mode";

    /// <summary>Label of the "follow the operating system" theme option.</summary>
    public string ThemeSystemLabel { get; set; } = "System";

    /// <summary>Label of the explicit light theme option.</summary>
    public string ThemeLightLabel { get; set; } = "Light";

    /// <summary>Label of the explicit dark theme option.</summary>
    public string ThemeDarkLabel { get; set; } = "Dark";

    /// <summary>Close-button label of a non-modal Alert.</summary>
    public string AlertCloseLabel { get; set; } = "Close alert";

    /// <summary>Close-button label of a Toast.</summary>
    public string ToastCloseLabel { get; set; } = "Close notification";

    /// <summary>Close-button label of a modal dialog.</summary>
    public string DialogCloseLabel { get; set; } = "Close dialog";

    /// <summary>Fallback name of a modal dialog without a title.</summary>
    public string DialogLabel { get; set; } = "Dialog";

    /// <summary>Dismiss-button label of a dismissible <c>Tag</c>.</summary>
    public string TagDismissLabel { get; set; } = "Remove tag";

    /// <summary>
    /// Copies the defaults of a fresh instance onto this one. Used by the option
    /// validation to repair empty entries without losing the host's overrides.
    /// </summary>
    internal void FillEmptyFrom(AeterniUITextOptions defaults)
    {
        ComboBoxPlaceholder = Or(ComboBoxPlaceholder, defaults.ComboBoxPlaceholder);
        ComboBoxListLabel = Or(ComboBoxListLabel, defaults.ComboBoxListLabel);
        RatingLabel = Or(RatingLabel, defaults.RatingLabel);
        ThemeSwitchLabel = Or(ThemeSwitchLabel, defaults.ThemeSwitchLabel);
        ThemeSystemLabel = Or(ThemeSystemLabel, defaults.ThemeSystemLabel);
        ThemeLightLabel = Or(ThemeLightLabel, defaults.ThemeLightLabel);
        ThemeDarkLabel = Or(ThemeDarkLabel, defaults.ThemeDarkLabel);
        AlertCloseLabel = Or(AlertCloseLabel, defaults.AlertCloseLabel);
        ToastCloseLabel = Or(ToastCloseLabel, defaults.ToastCloseLabel);
        DialogCloseLabel = Or(DialogCloseLabel, defaults.DialogCloseLabel);
        DialogLabel = Or(DialogLabel, defaults.DialogLabel);
        TagDismissLabel = Or(TagDismissLabel, defaults.TagDismissLabel);
    }

    private static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
