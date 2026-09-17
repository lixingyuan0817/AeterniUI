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
    [Parameter] public TimeSpan TimeStep { get; set; } = TimeSpan.FromMinutes(30);
    [Parameter] public TimeFormat TimeFormat { get; set; } = TimeFormat.Auto;
    [Parameter] public string? Format { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public DateTimeKind Kind { get; set; } = DateTimeKind.Unspecified;
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private DateOnly _displayMonth = FirstOfMonth(DateOnly.FromDateTime(DateTime.Today));
    private IReadOnlyList<TimeOnly> TimeOptions { get; set; } = [];
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private DateOnly? SelectedDate => Value.HasValue ? DateOnly.FromDateTime(Value.Value) : null;
    private TimeOnly? SelectedTime => Value.HasValue ? TimeOnly.FromDateTime(Value.Value) : null;
    private string ResolvedTimeFormat => TimePickerOptions.ResolveFormat(TimeFormat, Culture);
    private string ResolvedFormat => string.IsNullOrWhiteSpace(Format) ? $"yyyy-MM-dd {ResolvedTimeFormat}" : Format.Trim();
    private string DisplayValue => Value?.ToString(ResolvedFormat, Culture) ?? (string.IsNullOrWhiteSpace(Placeholder) ? UiText.DateTimePickerPlaceholder : Placeholder.Trim());
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.DateTimePickerLabel : AriaLabel.Trim();
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
        UpdateEditContextSubscription();
        if (Value.HasValue)
            _displayMonth = FirstOfMonth(DateOnly.FromDateTime(Value.Value));
        RefreshTimeOptions();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-date-time-picker")
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    private bool IsDateDisabled(DateOnly date)
    {
        if (DisabledDate?.Invoke(date) == true) return true;
        return BuildTimeOptions(date).Count == 0;
    }

    private async Task SelectDateAsync(DateOnly date)
    {
        if (IsDisabled || IsDateDisabled(date)) return;
        _displayMonth = FirstOfMonth(date);
        TimeOptions = BuildTimeOptions(date);
        var time = SelectedTime.HasValue && TimeOptions.Contains(SelectedTime.Value) ? SelectedTime.Value : TimeOptions[0];
        await SetValueAsync(CreateValue(date, time), close: false);
    }

    private async Task SelectTimeAsync(TimeOnly time)
    {
        var date = SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        if (IsDisabled || IsDateDisabled(date) || !BuildTimeOptions(date).Contains(time)) return;
        await SetValueAsync(CreateValue(date, time), close: true);
    }

    private async Task SetValueAsync(DateTime value, bool close)
    {
        Value = value;
        await ValueChanged.InvokeAsync(value);
        NotifyFieldChanged();
        _open = !close;
    }

    private IReadOnlyList<TimeOnly> BuildTimeOptions(DateOnly date) =>
        TimePickerOptions.Build(TimeStep, null, null, time => IsDateTimeDisabled(CreateValue(date, time)));

    private bool IsDateTimeDisabled(DateTime value) =>
        (MinDateTime.HasValue && value < MinDateTime.Value) ||
        (MaxDateTime.HasValue && value > MaxDateTime.Value) ||
        DisabledDateTime?.Invoke(value) == true;

    private void RefreshTimeOptions()
    {
        var date = SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);
        TimeOptions = DisabledDate?.Invoke(date) == true ? [] : BuildTimeOptions(date);
    }

    private DateTime CreateValue(DateOnly date, TimeOnly time) => DateTime.SpecifyKind(date.ToDateTime(time), Kind);
    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private Task SetMonthAsync(DateOnly month)
    {
        _displayMonth = FirstOfMonth(month);
        return Task.CompletedTask;
    }

    private Task ToggleAsync()
    {
        if (!IsDisabled) _open = !_open;
        return Task.CompletedTask;
    }

    private Task HandleOpenChanged(bool open)
    {
        _open = open;
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
