using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components.DatePicker;

public partial class DatePicker : AeterniComponent
{
    [CascadingParameter] private FormFieldContext? FormField { get; set; }
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    [Parameter] public DateOnly? Value { get; set; }
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<DateOnly?>>? ValueExpression { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    [Parameter] public DateOnly? MaxDate { get; set; }
    [Parameter] public Func<DateOnly, bool>? DisabledDate { get; set; }
    [Parameter] public string Format { get; set; } = "yyyy-MM-dd";
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private ElementReference _triggerElement;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private DateOnly _displayMonth = DateOnly.FromDateTime(DateTime.Today).AddDays(1 - DateTime.Today.Day);
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private string EffectivePlaceholder => string.IsNullOrWhiteSpace(Placeholder) ? UiText.DatePickerPlaceholder : Placeholder.Trim();
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.DatePickerLabel : AriaLabel.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifier && _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);
    private string? SizeClass => ComponentClass.ForSize("aeterni-date-picker", Size);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-date-picker")
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();
        if (Value.HasValue) _displayMonth = Value.Value.AddDays(1 - Value.Value.Day);
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    private bool IsDateDisabled(DateOnly date) =>
        (MinDate.HasValue && date < MinDate.Value) ||
        (MaxDate.HasValue && date > MaxDate.Value) ||
        (DisabledDate?.Invoke(date) ?? false);

    private async Task SelectAsync(DateOnly date)
    {
        if (IsDateDisabled(date)) return;
        Value = date;
        await ValueChanged.InvokeAsync(date);
        NotifyFieldChanged();
        _open = false;
    }

    private Task SetMonthAsync(DateOnly month)
    {
        _displayMonth = month;
        return InvokeAsync(StateHasChanged);
    }

    private Task ToggleAsync() { if (IsDisabled) return Task.CompletedTask; _open = !_open; return InvokeAsync(StateHasChanged); }
    private Task HandleOpenChanged(bool open) { _open = open; return InvokeAsync(StateHasChanged); }

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
        if (_hasFieldIdentifier && _subscribedEditContext is not null) _subscribedEditContext.NotifyFieldChanged(_fieldIdentifier);
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        if (_subscribedEditContext is not null) _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        return ValueTask.CompletedTask;
    }

}
