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

    private readonly EditContextSubscription _validation;

    public DatePicker() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

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
    [Parameter] public bool FullWidth { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private ElementReference _triggerElement;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private DateOnly _displayMonth = DateOnly.FromDateTime(DateTime.Today).AddDays(1 - DateTime.Today.Day);

    /// <summary>The value that last moved <see cref="_displayMonth" />.</summary>
    private DateOnly? _monthSourceValue;
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private string EffectivePlaceholder => string.IsNullOrWhiteSpace(Placeholder) ? UiText.DatePickerPlaceholder : Placeholder.Trim();
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.DatePickerLabel : AriaLabel.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));
    private string? SizeClass => ComponentClass.ForSize("aeterni-date-picker", Size);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-date-picker")
        .Add("is-full-width", FullWidth)
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();
        // REV-154: OnParametersSet runs on every parent render, so resetting the
        // month unconditionally snapped a month the user had paged to back to the
        // selected value's month as soon as any host state changed.
        if (Value != _monthSourceValue)
        {
            if (Value.HasValue) _displayMonth = Value.Value.AddDays(1 - Value.Value.Day);
            _monthSourceValue = Value;
        }
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown date picker placement.");
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown date picker size.");
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


    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }

}
