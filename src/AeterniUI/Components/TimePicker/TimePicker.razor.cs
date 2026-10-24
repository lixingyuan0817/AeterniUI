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

    /// <summary>
    /// Description ids supplied by the host. They are merged with the cascaded
    /// <c>FormField.DescribedBy</c> before reaching the trigger, and
    /// duplicate ids are emitted once.
    /// </summary>
    [Parameter] public string? AriaDescribedBy { get; set; }

    [Parameter] public string? OptionsAriaLabel { get; set; }
    [Parameter] public string? CancelText { get; set; }
    [Parameter] public string? ConfirmText { get; set; }
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>
    /// Keeps the trigger focusable and its text selectable, but never opens the time
    /// wheel and never rewrites the bound value. <c>Disabled</c> wins when both are
    /// set.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

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
    private string? EffectiveDescribedBy => MergeDescriptionIds(AriaDescribedBy, FormField?.DescribedBy);
    private string EffectiveOptionsLabel => string.IsNullOrWhiteSpace(OptionsAriaLabel) ? UiText.TimePickerOptionsLabel : OptionsAriaLabel.Trim();
    private string EffectiveCancelText => string.IsNullOrWhiteSpace(CancelText) ? UiText.TimePickerCancelText : CancelText.Trim();
    private string EffectiveConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? UiText.TimePickerConfirmText : ConfirmText.Trim();
    private string TriggerId => FormField?.InputId ?? $"{ElementId}-trigger";
    // Read-only is a component state, not a field state: FormField has no ReadOnly
    // cascade (same as ComboBox / MultiSelect / Autocomplete).
    private bool IsReadOnly => ReadOnly;
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

        // Read-only and disabled are state changes, not suggestions: an open wheel
        // closes instead of staying interactive behind the trigger.
        if (IsDisabled || IsReadOnly)
        {
            _open = false;
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-time-picker")
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", IsReadOnly)
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    private async Task SelectAsync(TimeOnly time)
    {
        if (IsDisabled || IsReadOnly) return;
        Value = time;
        await ValueChanged.InvokeAsync(time);
        NotifyFieldChanged();
        _open = false;
    }

    private Task ToggleAsync()
    {
        if (!IsDisabled && !IsReadOnly) _open = !_open;
        return Task.CompletedTask;
    }

    private Task HandleOpenChanged(bool open)
    {
        if (open && (IsDisabled || IsReadOnly))
        {
            // The popover only reports closures today, but the handler also guards a
            // future programmatic open: read-only must never reach the wheel.
            _open = false;
            return Task.CompletedTask;
        }

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
        if (IsDisabled || IsReadOnly) return;
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
