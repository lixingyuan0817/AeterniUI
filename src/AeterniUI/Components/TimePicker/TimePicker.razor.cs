using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.TimePicker;

public partial class TimePicker : AeterniComponent
{
    [CascadingParameter] private FormFieldContext? FormField { get; set; }
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public TimePicker() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter] public TimeOnly? Value { get; set; }
    [Parameter] public EventCallback<TimeOnly?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TimeOnly?>>? ValueExpression { get; set; }
    [Parameter] public TimeOnly? MinTime { get; set; }
    [Parameter] public TimeOnly? MaxTime { get; set; }
    [Parameter] public Func<TimeOnly, bool>? DisabledTime { get; set; }
    [Parameter] public TimeSpan Step { get; set; } = TimeSpan.FromSeconds(1);
    [Parameter] public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;
    [Parameter] public string? Format { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string? OptionsAriaLabel { get; set; }
    [Parameter] public string? CancelText { get; set; }
    [Parameter] public string? ConfirmText { get; set; }
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool FullWidth { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private TimeSelectionMap _selectionMap = default!;
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private string ResolvedFormat => string.IsNullOrWhiteSpace(Format)
        ? TimePickerOptions.ResolveFormat(TimeFormat, Culture, includeSeconds: true)
        : Format.Trim();
    private string DisplayValue => Value?.ToString(ResolvedFormat, Culture) ?? (string.IsNullOrWhiteSpace(Placeholder) ? UiText.TimePickerPlaceholder : Placeholder.Trim());
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.TimePickerLabel : AriaLabel.Trim();
    private string EffectiveOptionsLabel => string.IsNullOrWhiteSpace(OptionsAriaLabel) ? UiText.TimePickerOptionsLabel : OptionsAriaLabel.Trim();
    private string EffectiveCancelText => string.IsNullOrWhiteSpace(CancelText) ? UiText.TimePickerCancelText : CancelText.Trim();
    private string EffectiveConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? UiText.TimePickerConfirmText : ConfirmText.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    private bool IsDisabled => Disabled || FormField?.Disabled == true;
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));
    private string? SizeClass => ComponentClass.ForSize("aeterni-time-picker", Size);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(TimeFormat)) throw new ArgumentOutOfRangeException(nameof(TimeFormat), TimeFormat, "Unknown time picker format.");
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown time picker placement.");
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown time picker size.");
        _selectionMap = TimePickerOptions.CreateMap(Step, MinTime, MaxTime, DisabledTime, _selectionMap);
        UpdateEditContextSubscription();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-time-picker")
        .Add("is-full-width", FullWidth)
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    private async Task SelectAsync(TimeOnly time)
    {
        if (IsDisabled) return;
        Value = time;
        await ValueChanged.InvokeAsync(time);
        NotifyFieldChanged();
        _open = false;
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

    private Task CancelAsync()
    {
        _open = false;
        return Task.CompletedTask;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsDisabled) return;
        if (args.Key is "ArrowDown" or "ArrowUp")
        {
            var adjacent = _selectionMap.FindAdjacent(Value, args.Key == "ArrowDown" ? 1 : -1);
            if (adjacent.HasValue) await SelectAsync(adjacent.Value);
        }
        else if (args.Key == "Escape")
        {
            _open = false;
        }
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


    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }
}
