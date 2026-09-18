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
    [Parameter] public string? CancelText { get; set; }
    [Parameter] public string? ConfirmText { get; set; }
    [Parameter] public DateTimeKind Kind { get; set; } = DateTimeKind.Unspecified;
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool FullWidth { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private DateTime? _draftValue;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private readonly Dictionary<DateOnly, bool> _dateDisabledCache = [];
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
    private string EffectiveCancelText => string.IsNullOrWhiteSpace(CancelText) ? UiText.TimePickerCancelText : CancelText.Trim();
    private string EffectiveConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? UiText.TimePickerConfirmText : ConfirmText.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    private bool IsDisabled => Disabled || FormField?.Disabled == true;
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifier && _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);
    private string? SizeClass => ComponentClass.ForSize("aeterni-date-time-picker", Size);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(TimeFormat)) throw new ArgumentOutOfRangeException(nameof(TimeFormat));
        if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind));
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (MinDateTime.HasValue && MaxDateTime.HasValue && MinDateTime.Value > MaxDateTime.Value)
            throw new ArgumentException("The minimum date and time cannot be later than the maximum date and time.");
        TimePickerOptions.Validate(TimeStep, null, null);
        _dateDisabledCache.Clear();
        UpdateEditContextSubscription();
        if (Value.HasValue)
            _displayMonth = FirstOfMonth(DateOnly.FromDateTime(Value.Value));
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-date-time-picker")
        .Add("is-full-width", FullWidth)
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    private bool IsDateDisabled(DateOnly date)
    {
        if (_dateDisabledCache.TryGetValue(date, out var cached))
        {
            return cached;
        }

        var disabled = DisabledDate?.Invoke(date) == true ||
            (MinDateTime.HasValue && date < DateOnly.FromDateTime(MinDateTime.Value)) ||
            (MaxDateTime.HasValue && date > DateOnly.FromDateTime(MaxDateTime.Value)) ||
            !TimePickerOptions.HasAvailable(
                TimeStep,
                MinimumTime(date),
                MaximumTime(date),
                time => DisabledDateTime?.Invoke(CreateValue(date, time)) == true);
        _dateDisabledCache[date] = disabled;
        return disabled;
    }

    private async Task SelectDateAsync(DateOnly date)
    {
        if (IsDisabled || IsDateDisabled(date)) return;
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
        if (IsDisabled || IsDateDisabled(date) || !BuildTimeMap(date).Contains(time)) return;
        await SetValueAsync(CreateValue(date, time), close: true);
    }

    private Task UpdateDraftTimeAsync(TimeOnly time)
    {
        var date = SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        _draftValue = CreateValue(date, time);
        return Task.CompletedTask;
    }

    private async Task SetValueAsync(DateTime value, bool close)
    {
        Value = value;
        await ValueChanged.InvokeAsync(value);
        NotifyFieldChanged();
        _open = !close;
    }

    private TimeSelectionMap BuildTimeMap(DateOnly date) =>
        TimePickerOptions.CreateMap(
            TimeStep,
            MinimumTime(date),
            MaximumTime(date),
            time => DisabledDateTime?.Invoke(CreateValue(date, time)) == true);

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
        if (!IsDisabled)
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
        var next = ValueExpression is null ? null : CascadedEditContext;
        if (!ReferenceEquals(_subscribedEditContext, next))
        {
            if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
            _subscribedEditContext = next;
            if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
        }
        _hasFieldIdentifier = ValueExpression is not null;
        if (_hasFieldIdentifier) _fieldIdentifier = FieldIdentifier.Create(ValueExpression!);
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        if (!IsDisposed) _ = InvokeAsync(StateHasChanged);
    }

    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier) _subscribedEditContext?.NotifyFieldChanged(_fieldIdentifier);
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        return ValueTask.CompletedTask;
    }
}
