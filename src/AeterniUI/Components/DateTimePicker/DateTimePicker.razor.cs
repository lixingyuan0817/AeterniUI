using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Components.DatePicker;
using AeterniUI.Components.FormField;
using AeterniUI.Components.TimePicker;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components.DateTimePicker;

public partial class DateTimePicker : AeterniComponent
{
    [CascadingParameter] private FormFieldContext? FormField { get; set; }
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public DateTimePicker()
    {
        _validation = new EditContextSubscription(RequestStateHasChanged);
        // One stable delegate: see DisabledDateTimeProbe (REV-161).
        _disabledTimeProbe = time => DisabledDateTime?.Invoke(CreateValue(ProbeDate, time)) == true;
    }

    [Parameter] public DateTime? Value { get; set; }
    [Parameter] public EventCallback<DateTime?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<DateTime?>>? ValueExpression { get; set; }
    [Parameter] public DateTime? MinDateTime { get; set; }
    [Parameter] public DateTime? MaxDateTime { get; set; }
    [Parameter] public Func<DateOnly, bool>? DisabledDate { get; set; }
    [Parameter] public Func<DateTime, bool>? DisabledDateTime { get; set; }
    [Parameter] public TimeSpan TimeStep { get; set; } = TimeSpan.FromSeconds(1);
    [Parameter] public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;
    [Parameter] public string? Format { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Description ids supplied by the host. They are merged with the cascaded
    /// <c>FormField.DescribedBy</c> before reaching the trigger, and
    /// duplicate ids are emitted once.
    /// </summary>
    [Parameter] public string? AriaDescribedBy { get; set; }

    [Parameter] public string? CancelText { get; set; }
    [Parameter] public string? ConfirmText { get; set; }
    [Parameter] public DateTimeKind Kind { get; set; } = DateTimeKind.Unspecified;
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>
    /// Keeps the trigger focusable and its text selectable, but never opens the
    /// calendar/time panel and never rewrites the bound value. <c>Disabled</c> wins
    /// when both are set.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Shows the Chinese lunisolar day under the Gregorian number in the calendar.</summary>
    [Parameter] public bool ShowLunar { get; set; }

    private bool _open;
    private DateTime? _draftValue;
    private DateTime? _lastParameterValue;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private readonly Dictionary<DateOnly, bool> _dateDisabledCache = [];

    /// <summary>
    /// Reference identity of everything the day grid reads. The cache is only thrown
    /// away when one of these changes, which is what keeps a parent render from
    /// re-scanning 42 days (REV-161).
    /// </summary>
    private (Func<DateTime, bool>? DisabledDateTime, Func<DateOnly, bool>? DisabledDate, TimeSpan Step, DateTime? Min, DateTime? Max)? _disabledDateCacheKey;

    /// <summary>
    /// Stable probe instance so the time map's own reuse check can hit: a fresh lambda
    /// per render made <c>CreateMap</c>'s cache unreachable (REV-161).
    /// </summary>
    private readonly Func<TimeOnly, bool> _disabledTimeProbe;
    private Func<TimeOnly, bool> DisabledDateTimeProbe => _disabledTimeProbe;

    /// <summary>
    /// The day whose grid is currently being built. The probe is created once (so it
    /// can be cached) and reads this field instead of closing over a changing date.
    /// </summary>
    private DateOnly ProbeDate { get; set; }
    private DateOnly _displayMonth = FirstOfMonth(DateOnly.FromDateTime(DateTime.Today));
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private DateTime? WorkingValue => _open ? _draftValue : Value;
    private DateOnly? SelectedDate => WorkingValue.HasValue ? DateOnly.FromDateTime(WorkingValue.Value) : null;
    private TimeOnly? SelectedTime => WorkingValue.HasValue ? TimeOnly.FromDateTime(WorkingValue.Value) : null;
    private DateOnly TimePanelDate => SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);
    private string ResolvedTimeFormat => TimePickerOptions.ResolveFormat(TimeFormat, Culture, includeSeconds: true);
    private string ResolvedFormat => string.IsNullOrWhiteSpace(Format) ? $"yyyy-MM-dd {ResolvedTimeFormat}" : Format.Trim();
    private string DisplayValue => Value?.ToString(ResolvedFormat, Culture) ?? (string.IsNullOrWhiteSpace(Placeholder) ? UiText.DateTimePickerPlaceholder : Placeholder.Trim());
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.DateTimePickerLabel : AriaLabel.Trim();
    private string? EffectiveDescribedBy => MergeDescriptionIds(AriaDescribedBy, FormField?.DescribedBy);
    private string EffectiveCancelText => string.IsNullOrWhiteSpace(CancelText) ? UiText.TimePickerCancelText : CancelText.Trim();
    private string EffectiveConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? UiText.TimePickerConfirmText : ConfirmText.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    // Read-only is a component state, not a field state: FormField has no ReadOnly
    // cascade (same as ComboBox / MultiSelect / Autocomplete).
    private bool IsReadOnly => ReadOnly;
    private bool IsDisabled => Disabled || FormField?.Disabled == true;
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));
    private string? SizeClass => ComponentClass.ForSize("aeterni-date-time-picker", Size);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(TimeFormat)) throw new ArgumentOutOfRangeException(nameof(TimeFormat), TimeFormat, "Unknown date time picker format.");
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown date time picker kind.");
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown date time picker placement.");
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown date time picker size.");
        if (MinDateTime.HasValue && MaxDateTime.HasValue && MinDateTime.Value > MaxDateTime.Value)
            throw new ArgumentException("The minimum date and time cannot be later than the maximum date and time.");
        TimePickerOptions.Validate(TimeStep, null, null);
        if (Value != _lastParameterValue)
        {
            _draftValue = Value;
            // REV-154: the visible month follows a real value change only; a parent
            // render used to pull the calendar back to the selected date's month.
            if (Value.HasValue)
            {
                _displayMonth = FirstOfMonth(DateOnly.FromDateTime(Value.Value));
            }
        }

        _lastParameterValue = Value;
        // REV-161: the day grid asks for up to 42 days per render, so clearing the
        // cache on every parameter set threw the work away even when nothing that
        // feeds it had changed.
        InvalidateDisabledDateCacheIfNeeded();
        UpdateEditContextSubscription();

        // Read-only and disabled are state changes, not suggestions: an open panel
        // closes instead of staying interactive behind the trigger.
        if (IsDisabled || IsReadOnly)
        {
            _open = false;
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-date-time-picker")
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", IsReadOnly)
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    private bool IsDateDisabled(DateOnly date)
    {
        if (_dateDisabledCache.TryGetValue(date, out var cached))
        {
            return cached;
        }

        ProbeDate = date;
        var window = MinimumTime(date) ?? MaximumTime(date);
        var disabled = DisabledDate?.Invoke(date) == true ||
            (MinDateTime.HasValue && date < DateOnly.FromDateTime(MinDateTime.Value)) ||
            (MaxDateTime.HasValue && date > DateOnly.FromDateTime(MaxDateTime.Value)) ||
            // REV-161: without a DisabledDateTime delegate and without a bound day the
            // time grid cannot exclude anything, so the per-second scan (86,400 steps
            // for a one-second step) is skipped entirely.
            (DisabledDateTime is not null || window is not null
                ? !TimePickerOptions.HasAvailable(
                    TimeStep,
                    MinimumTime(date),
                    MaximumTime(date),
                    DisabledDateTimeProbe)
                : false);
        _dateDisabledCache[date] = disabled;
        return disabled;
    }

    private async Task SelectDateAsync(DateOnly date)
    {
        if (IsDisabled || IsReadOnly || IsDateDisabled(date)) return;
        _displayMonth = FirstOfMonth(date);
        var map = BuildTimeMap(date);
        var time = SelectedTime.HasValue && map.Contains(SelectedTime.Value)
            ? SelectedTime.Value
            : map.FindClosest(SelectedTime);
        if (time.HasValue)
        {
            _draftValue = CreateValue(date, time.Value);
        }

        await Task.CompletedTask;
    }

    private async Task SelectTimeAsync(TimeOnly time)
    {
        var date = SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        if (IsDisabled || IsReadOnly || IsDateDisabled(date) || !BuildTimeMap(date).Contains(time)) return;
        await SetValueAsync(CreateValue(date, time), close: true);
    }

    private Task UpdateDraftTimeAsync(TimeOnly time)
    {
        if (IsDisabled || IsReadOnly) return Task.CompletedTask;
        var date = SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        _draftValue = CreateValue(date, time);
        return Task.CompletedTask;
    }

    private async Task SetValueAsync(DateTime value, bool close)
    {
        if (IsDisabled || IsReadOnly) return;
        Value = value;
        await ValueChanged.InvokeAsync(value);
        NotifyFieldChanged();
        _open = !close;
    }

    private TimeSelectionMap BuildTimeMap(DateOnly date)
    {
        ProbeDate = date;
        return TimePickerOptions.CreateMap(
            TimeStep,
            MinimumTime(date),
            MaximumTime(date),
            DisabledDateTime is null ? null : DisabledDateTimeProbe);
    }

    private void InvalidateDisabledDateCacheIfNeeded()
    {
        var key = (DisabledDateTime, DisabledDate, TimeStep, MinDateTime, MaxDateTime);
        if (_disabledDateCacheKey == key)
        {
            return;
        }

        _disabledDateCacheKey = key;
        _dateDisabledCache.Clear();
    }

    private TimeOnly? MinimumTime(DateOnly date) =>
        MinDateTime.HasValue && DateOnly.FromDateTime(MinDateTime.Value) == date
            ? TimeOnly.FromDateTime(MinDateTime.Value)
            : null;

    private TimeOnly? MaximumTime(DateOnly date) =>
        MaxDateTime.HasValue && DateOnly.FromDateTime(MaxDateTime.Value) == date
            ? TimeOnly.FromDateTime(MaxDateTime.Value)
            : null;

    private bool IsPanelTimeDisabled(TimeOnly time) =>
        DisabledDate?.Invoke(TimePanelDate) == true || DisabledDateTime?.Invoke(CreateValue(TimePanelDate, time)) == true;

    private DateTime CreateValue(DateOnly date, TimeOnly time) => DateTime.SpecifyKind(date.ToDateTime(time), Kind);
    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private Task SetMonthAsync(DateOnly month)
    {
        _displayMonth = FirstOfMonth(month);
        return Task.CompletedTask;
    }

    private Task ToggleAsync()
    {
        if (!IsDisabled && !IsReadOnly)
        {
            if (!_open)
            {
                _draftValue = Value;
            }

            _open = !_open;
        }
        return Task.CompletedTask;
    }

    private Task HandleOpenChanged(bool open)
    {
        if (open && (IsDisabled || IsReadOnly))
        {
            // The popover only reports closures today, but the handler also guards a
            // future programmatic open: read-only must never reach the panel.
            _open = false;
            return Task.CompletedTask;
        }

        if (open && !_open)
        {
            _draftValue = Value;
        }

        _open = open;
        return Task.CompletedTask;
    }

    private Task CancelAsync()
    {
        _draftValue = Value;
        _open = false;
        return Task.CompletedTask;
    }

    private void UpdateEditContextSubscription()
    {
        _validation.Attach(ValueExpression is null ? null : CascadedEditContext);
        _hasFieldIdentifier = ValueExpression is not null;
        if (_hasFieldIdentifier) _fieldIdentifier = FieldIdentifier.Create(ValueExpression!);
    }

    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier)
        {
            _validation.NotifyFieldChanged(_fieldIdentifier);
        }
    }


    /// <summary>
    /// Merges the host description ids with the field-provided ones in the order the
    /// other field controls use (host first, then <c>FormField</c>) and drops ids that
    /// were already listed by one of the two sources.
    /// </summary>
    private static string? MergeDescriptionIds(string? hostDescribedBy, string? fieldDescribedBy)
    {
        var merged = string.Join(' ', new[] { hostDescribedBy, fieldDescribedBy }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal));
        return merged.Length == 0 ? null : merged;
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }
}
