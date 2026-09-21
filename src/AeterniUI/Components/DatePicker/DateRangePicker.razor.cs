using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components.DatePicker;

public partial class DateRangePicker : AeterniComponent
{
    [CascadingParameter] private FormFieldContext? FormField { get; set; }
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    [Parameter] public DateOnly? StartDate { get; set; }
    [Parameter] public EventCallback<DateOnly?> StartDateChanged { get; set; }
    [Parameter] public DateOnly? EndDate { get; set; }
    [Parameter] public EventCallback<DateOnly?> EndDateChanged { get; set; }
    [Parameter] public Expression<Func<DateOnly?>>? StartDateExpression { get; set; }
    [Parameter] public Expression<Func<DateOnly?>>? EndDateExpression { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    [Parameter] public DateOnly? MaxDate { get; set; }
    [Parameter] public Func<DateOnly, bool>? DisabledDate { get; set; }
    [Parameter] public IReadOnlyList<DateRangePreset> Presets { get; set; } = [];
    [Parameter] public int VisibleMonths { get; set; } = 1;
    [Parameter] public string? PresetsAriaLabel { get; set; }
    [Parameter] public string Format { get; set; } = "yyyy-MM-dd";
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private DateOnly? _hoverDate;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _startFieldIdentifier;
    private FieldIdentifier _endFieldIdentifier;
    private bool _hasFieldIdentifiers;
    private DateOnly _displayMonth = DateOnly.FromDateTime(DateTime.Today).AddDays(1 - DateTime.Today.Day);
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private IReadOnlyList<DateRangePreset> PresetItems => Presets ?? [];
    private string EffectivePlaceholder => string.IsNullOrWhiteSpace(Placeholder) ? UiText.DatePickerPlaceholder : Placeholder.Trim();
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.DateRangePickerLabel : AriaLabel.Trim();
    private string EffectivePresetsAriaLabel => string.IsNullOrWhiteSpace(PresetsAriaLabel) ? UiText.DateRangePickerPresetsLabel : PresetsAriaLabel.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifiers &&
        ((StartDateExpression is not null && _subscribedEditContext?.GetValidationMessages(_startFieldIdentifier).Any() == true) ||
         (EndDateExpression is not null && _subscribedEditContext?.GetValidationMessages(_endFieldIdentifier).Any() == true)));
    private string? SizeClass => ComponentClass.ForSize("aeterni-date-picker", Size);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-date-range-picker")
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();
        if (StartDate.HasValue) _displayMonth = StartDate.Value.AddDays(1 - StartDate.Value.Day);
        if (VisibleMonths is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(VisibleMonths), VisibleMonths, "Visible months must be between 1 and 3.");
        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
            throw new ArgumentException("The minimum date cannot be later than the maximum date.");
        ValidatePresets();
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    private bool IsDateDisabled(DateOnly date) =>
        (MinDate.HasValue && date < MinDate.Value) || (MaxDate.HasValue && date > MaxDate.Value) || (DisabledDate?.Invoke(date) ?? false);

    private async Task SelectAsync(DateOnly date)
    {
        if (IsDisabled || IsDateDisabled(date)) return;
        if (!StartDate.HasValue || EndDate.HasValue || date < StartDate.Value)
        {
            StartDate = date; EndDate = null;
            _hoverDate = null;
            await StartDateChanged.InvokeAsync(date); await EndDateChanged.InvokeAsync(null);
            NotifyFieldChanged();
        }
        else
        {
            EndDate = date; await EndDateChanged.InvokeAsync(date); NotifyFieldChanged(); _open = false;
        }
    }

    private bool IsPresetSelected(DateRangePreset preset) => StartDate == preset.StartDate && EndDate == preset.EndDate;

    private async Task SelectPresetAsync(DateRangePreset preset)
    {
        if (IsDisabled) return;
        StartDate = preset.StartDate;
        EndDate = preset.EndDate;
        _displayMonth = preset.StartDate.AddDays(1 - preset.StartDate.Day);
        _hoverDate = null;
        await StartDateChanged.InvokeAsync(StartDate);
        await EndDateChanged.InvokeAsync(EndDate);
        NotifyFieldChanged();
        _open = false;
    }

    private void ValidatePresets()
    {
        foreach (var preset in PresetItems)
        {
            if (string.IsNullOrWhiteSpace(preset.Label))
                throw new ArgumentException("Date range preset labels cannot be empty.", nameof(Presets));
            if (preset.StartDate > preset.EndDate)
                throw new ArgumentException($"The preset '{preset.Label}' starts after it ends.", nameof(Presets));
            if (IsDateDisabled(preset.StartDate) || IsDateDisabled(preset.EndDate))
                throw new ArgumentException($"The preset '{preset.Label}' contains a disabled or out-of-range date.", nameof(Presets));
            if (DisabledDate is null) continue;
            for (var day = preset.StartDate.DayNumber; day <= preset.EndDate.DayNumber; day++)
            {
                var date = DateOnly.FromDayNumber(day);
                if (DisabledDate(date))
                    throw new ArgumentException($"The preset '{preset.Label}' contains a disabled date.", nameof(Presets));
                if (date == DateOnly.MaxValue) break;
            }
        }
    }

    private Task SetMonthAsync(DateOnly month) { _displayMonth = month; return InvokeAsync(StateHasChanged); }
    private Task HandleHoverAsync(DateOnly date) { _hoverDate = date; return InvokeAsync(StateHasChanged); }
    private Task ToggleAsync() { if (IsDisabled) return Task.CompletedTask; _open = !_open; return InvokeAsync(StateHasChanged); }
    private Task HandleOpenChanged(bool open) { _open = open; return InvokeAsync(StateHasChanged); }

    private void UpdateEditContextSubscription()
    {
        var next = CascadedEditContext;
        if (!ReferenceEquals(_subscribedEditContext, next))
        {
            if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
            _subscribedEditContext = next;
            if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
        }
        _hasFieldIdentifiers = StartDateExpression is not null || EndDateExpression is not null;
        if (StartDateExpression is not null) _startFieldIdentifier = FieldIdentifier.Create(StartDateExpression);
        if (EndDateExpression is not null) _endFieldIdentifier = FieldIdentifier.Create(EndDateExpression);
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        if (!IsDisposed) _ = InvokeAsync(StateHasChanged);
    }

    private void NotifyFieldChanged()
    {
        if (!_hasFieldIdentifiers || _subscribedEditContext is null) return;
        if (StartDateExpression is not null) _subscribedEditContext.NotifyFieldChanged(_startFieldIdentifier);
        if (EndDateExpression is not null) _subscribedEditContext.NotifyFieldChanged(_endFieldIdentifier);
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        return ValueTask.CompletedTask;
    }

}
