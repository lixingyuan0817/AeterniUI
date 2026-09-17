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

    [Parameter] public TimeOnly? Value { get; set; }
    [Parameter] public EventCallback<TimeOnly?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TimeOnly?>>? ValueExpression { get; set; }
    [Parameter] public TimeOnly? MinTime { get; set; }
    [Parameter] public TimeOnly? MaxTime { get; set; }
    [Parameter] public Func<TimeOnly, bool>? DisabledTime { get; set; }
    [Parameter] public TimeSpan Step { get; set; } = TimeSpan.FromMinutes(30);
    [Parameter] public TimeFormat TimeFormat { get; set; } = TimeFormat.Auto;
    [Parameter] public string? Format { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string? OptionsAriaLabel { get; set; }
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }

    private bool _open;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private IReadOnlyList<TimeOnly> Options { get; set; } = [];
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private string ResolvedFormat => string.IsNullOrWhiteSpace(Format) ? TimePickerOptions.ResolveFormat(TimeFormat, Culture) : Format.Trim();
    private string DisplayValue => Value?.ToString(ResolvedFormat, Culture) ?? (string.IsNullOrWhiteSpace(Placeholder) ? UiText.TimePickerPlaceholder : Placeholder.Trim());
    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.TimePickerLabel : AriaLabel.Trim();
    private string EffectiveOptionsLabel => string.IsNullOrWhiteSpace(OptionsAriaLabel) ? UiText.TimePickerOptionsLabel : OptionsAriaLabel.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    private bool IsDisabled => Disabled || FormField?.Disabled == true;
    private bool IsRequired => Required || FormField?.Required == true;
    private bool IsInvalid => Invalid || FormField?.Invalid == true || (_hasFieldIdentifier && _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);
    private string? SizeClass => ComponentClass.ForSize("aeterni-time-picker", Size);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(TimeFormat)) throw new ArgumentOutOfRangeException(nameof(TimeFormat));
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        Options = TimePickerOptions.Build(Step, MinTime, MaxTime, DisabledTime);
        UpdateEditContextSubscription();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-time-picker")
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

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsDisabled) return;
        if (args.Key is "ArrowDown" or "ArrowUp")
        {
            var adjacent = TimePickerOptions.FindAdjacent(Options, Value, args.Key == "ArrowDown" ? 1 : -1);
            if (adjacent.HasValue) await SelectAsync(adjacent.Value);
        }
        else if (args.Key == "Escape")
        {
            _open = false;
        }
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
