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

    /// <summary>Trigger text of a <c>TimePicker</c> that has no value yet.</summary>
    public string TimePickerPlaceholder { get; set; } = "Select time";

    /// <summary>Accessible name of a <c>TimePicker</c>.</summary>
    public string TimePickerLabel { get; set; } = "Time picker";

    /// <summary>Accessible name of the available time option list.</summary>
    public string TimePickerOptionsLabel { get; set; } = "Available times";

    /// <summary>Message shown when no time option is available.</summary>
    public string TimePickerEmptyText { get; set; } = "No available times";

    /// <summary>Visible and accessible label of the hour column.</summary>
    public string TimePickerHourLabel { get; set; } = "Hour";

    /// <summary>Visible and accessible label of the minute column.</summary>
    public string TimePickerMinuteLabel { get; set; } = "Minute";

    /// <summary>Visible and accessible label of the second column.</summary>
    public string TimePickerSecondLabel { get; set; } = "Second";

    /// <summary>Label of the time picker's cancel action.</summary>
    public string TimePickerCancelText { get; set; } = "Cancel";

    /// <summary>Label of the time picker's confirm action.</summary>
    public string TimePickerConfirmText { get; set; } = "Confirm";

    /// <summary>Trigger text of a <c>DateTimePicker</c> that has no value yet.</summary>
    public string DateTimePickerPlaceholder { get; set; } = "Select date and time";

    /// <summary>Accessible name of a <c>DateTimePicker</c>.</summary>
    public string DateTimePickerLabel { get; set; } = "Date and time picker";

    /// <summary>Accessible name of a date calendar.</summary>
    public string DatePickerCalendarLabel { get; set; } = "Calendar";

    /// <summary>Trigger text of a <c>DatePicker</c> that has no value yet.</summary>
    public string DatePickerPlaceholder { get; set; } = "Select date";

    /// <summary>Accessible name of a <c>DatePicker</c>.</summary>
    public string DatePickerLabel { get; set; } = "Date picker";

    /// <summary>Accessible name of a <c>DateRangePicker</c>.</summary>
    public string DateRangePickerLabel { get; set; } = "Date range picker";

    /// <summary>Accessible name of the quick-range group.</summary>
    public string DateRangePickerPresetsLabel { get; set; } = "Quick ranges";

    /// <summary>Accessible name of the previous-month calendar action.</summary>
    public string DatePickerPreviousMonthLabel { get; set; } = "Previous month";

    /// <summary>Accessible name of the next-month calendar action.</summary>
    public string DatePickerNextMonthLabel { get; set; } = "Next month";

    /// <summary>Accessible name of a pagination navigation region.</summary>
    public string PaginationLabel { get; set; } = "Pagination";

    /// <summary>Accessible name of the previous-page action.</summary>
    public string PaginationPreviousLabel { get; set; } = "Previous page";

    /// <summary>Accessible name of the next-page action.</summary>
    public string PaginationNextLabel { get; set; } = "Next page";

    /// <summary>
    /// Composite format for a page button's accessible name. Placeholder {0}
    /// receives the one-based page number.
    /// </summary>
    public string PaginationPageLabelFormat { get; set; } = "Page {0}";

    /// <summary>Accessible name of a <c>Rating</c> group.</summary>
    public string RatingLabel { get; set; } = "Rating";

    /// <summary>Accessible name of a <c>Tabs</c> strip.</summary>
    public string TabsLabel { get; set; } = "Tabs";

    /// <summary>Accessible name of a <c>Breadcrumb</c> navigation trail.</summary>
    public string BreadcrumbLabel { get; set; } = "Breadcrumb";

    /// <summary>Accessible name of a <c>Stepper</c> process indicator.</summary>
    public string StepperLabel { get; set; } = "Progress steps";

    /// <summary>Accessible name of the <c>ThemeSwitch</c> group.</summary>
    public string ThemeSwitchLabel { get; set; } = "Theme mode";

    /// <summary>Label of the "follow the operating system" theme option.</summary>
    public string ThemeSystemLabel { get; set; } = "System";

    /// <summary>Label of the explicit light theme option.</summary>
    public string ThemeLightLabel { get; set; } = "Light";

    /// <summary>Label of the explicit dark theme option.</summary>
    public string ThemeDarkLabel { get; set; } = "Dark";

    /// <summary>Accessible name of the <c>ThemeBrandSwitch</c> group.</summary>
    public string ThemeBrandSwitchLabel { get; set; } = "Brand color";

    /// <summary>Label of the purple brand hue option.</summary>
    public string ThemeBrandPurpleLabel { get; set; } = "Purple";

    /// <summary>Label of the green brand hue option.</summary>
    public string ThemeBrandGreenLabel { get; set; } = "Green";

    /// <summary>Label of the orange brand hue option.</summary>
    public string ThemeBrandOrangeLabel { get; set; } = "Orange";

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

    /// <summary>Accessible name of a <c>Spinner</c>.</summary>
    public string SpinnerLabel { get; set; } = "Loading";

    /// <summary>Heading of an <c>Empty</c> placeholder that has no title.</summary>
    public string EmptyTitle { get; set; } = "No data";

    /// <summary>Accessible name of a dot-mode <c>Badge</c> indicator.</summary>
    public string BadgeLabel { get; set; } = "New";

    /// <summary>Fallback name of a <c>Drawer</c> without a title.</summary>
    public string DrawerLabel { get; set; } = "Panel";

    /// <summary>Close-button label of a <c>Drawer</c>.</summary>
    public string DrawerCloseLabel { get; set; } = "Close panel";

    /// <summary>
    /// Copies the defaults of a fresh instance onto this one. Used by the option
    /// validation to repair empty entries without losing the host's overrides.
    /// </summary>
    internal void FillEmptyFrom(AeterniUITextOptions defaults)
    {
        ComboBoxPlaceholder = Or(ComboBoxPlaceholder, defaults.ComboBoxPlaceholder);
        ComboBoxListLabel = Or(ComboBoxListLabel, defaults.ComboBoxListLabel);
        TimePickerPlaceholder = Or(TimePickerPlaceholder, defaults.TimePickerPlaceholder);
        TimePickerLabel = Or(TimePickerLabel, defaults.TimePickerLabel);
        TimePickerOptionsLabel = Or(TimePickerOptionsLabel, defaults.TimePickerOptionsLabel);
        TimePickerEmptyText = Or(TimePickerEmptyText, defaults.TimePickerEmptyText);
        TimePickerHourLabel = Or(TimePickerHourLabel, defaults.TimePickerHourLabel);
        TimePickerMinuteLabel = Or(TimePickerMinuteLabel, defaults.TimePickerMinuteLabel);
        TimePickerSecondLabel = Or(TimePickerSecondLabel, defaults.TimePickerSecondLabel);
        TimePickerCancelText = Or(TimePickerCancelText, defaults.TimePickerCancelText);
        TimePickerConfirmText = Or(TimePickerConfirmText, defaults.TimePickerConfirmText);
        DateTimePickerPlaceholder = Or(DateTimePickerPlaceholder, defaults.DateTimePickerPlaceholder);
        DateTimePickerLabel = Or(DateTimePickerLabel, defaults.DateTimePickerLabel);
        DatePickerCalendarLabel = Or(DatePickerCalendarLabel, defaults.DatePickerCalendarLabel);
        DatePickerPlaceholder = Or(DatePickerPlaceholder, defaults.DatePickerPlaceholder);
        DatePickerLabel = Or(DatePickerLabel, defaults.DatePickerLabel);
        DateRangePickerLabel = Or(DateRangePickerLabel, defaults.DateRangePickerLabel);
        DateRangePickerPresetsLabel = Or(DateRangePickerPresetsLabel, defaults.DateRangePickerPresetsLabel);
        DatePickerPreviousMonthLabel = Or(DatePickerPreviousMonthLabel, defaults.DatePickerPreviousMonthLabel);
        DatePickerNextMonthLabel = Or(DatePickerNextMonthLabel, defaults.DatePickerNextMonthLabel);
        PaginationLabel = Or(PaginationLabel, defaults.PaginationLabel);
        PaginationPreviousLabel = Or(PaginationPreviousLabel, defaults.PaginationPreviousLabel);
        PaginationNextLabel = Or(PaginationNextLabel, defaults.PaginationNextLabel);
        PaginationPageLabelFormat = Or(PaginationPageLabelFormat, defaults.PaginationPageLabelFormat);
        RatingLabel = Or(RatingLabel, defaults.RatingLabel);
        TabsLabel = Or(TabsLabel, defaults.TabsLabel);
        BreadcrumbLabel = Or(BreadcrumbLabel, defaults.BreadcrumbLabel);
        StepperLabel = Or(StepperLabel, defaults.StepperLabel);
        ThemeSwitchLabel = Or(ThemeSwitchLabel, defaults.ThemeSwitchLabel);
        ThemeSystemLabel = Or(ThemeSystemLabel, defaults.ThemeSystemLabel);
        ThemeLightLabel = Or(ThemeLightLabel, defaults.ThemeLightLabel);
        ThemeDarkLabel = Or(ThemeDarkLabel, defaults.ThemeDarkLabel);
        ThemeBrandSwitchLabel = Or(ThemeBrandSwitchLabel, defaults.ThemeBrandSwitchLabel);
        ThemeBrandPurpleLabel = Or(ThemeBrandPurpleLabel, defaults.ThemeBrandPurpleLabel);
        ThemeBrandGreenLabel = Or(ThemeBrandGreenLabel, defaults.ThemeBrandGreenLabel);
        ThemeBrandOrangeLabel = Or(ThemeBrandOrangeLabel, defaults.ThemeBrandOrangeLabel);
        AlertCloseLabel = Or(AlertCloseLabel, defaults.AlertCloseLabel);
        ToastCloseLabel = Or(ToastCloseLabel, defaults.ToastCloseLabel);
        DialogCloseLabel = Or(DialogCloseLabel, defaults.DialogCloseLabel);
        DialogLabel = Or(DialogLabel, defaults.DialogLabel);
        TagDismissLabel = Or(TagDismissLabel, defaults.TagDismissLabel);
        SpinnerLabel = Or(SpinnerLabel, defaults.SpinnerLabel);
        EmptyTitle = Or(EmptyTitle, defaults.EmptyTitle);
        BadgeLabel = Or(BadgeLabel, defaults.BadgeLabel);
        DrawerLabel = Or(DrawerLabel, defaults.DrawerLabel);
        DrawerCloseLabel = Or(DrawerCloseLabel, defaults.DrawerCloseLabel);
    }

    private static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
