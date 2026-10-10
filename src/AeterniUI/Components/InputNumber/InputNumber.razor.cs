using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Icons;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.InputNumber;

/// <summary>
/// A single-line numeric field with step-based keyboard and button controls.
/// The value is kept as a <see cref="TValue" /> and bound like any form control;
/// typed text is parsed with the current culture and only reshaped on commit.
/// </summary>
[JsModule("Components/InputNumber/InputNumber.razor.js", Name = "inputnumber", Interactive = true)]
public partial class InputNumber<TValue> : AeterniComponent
    where TValue : struct, INumber<TValue>
{
    /// <summary>
    /// PageUp/PageDown step this many increments at once, so coarse ends of a
    /// range stay reachable without holding an arrow key.
    /// </summary>
    private const int PageStepMultiplier = 10;

    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public InputNumber() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Lower bound; <see langword="null" /> leaves the value unbounded.</summary>
    [Parameter]
    public TValue? Min { get; set; }

    /// <summary>Upper bound; <see langword="null" /> leaves the value unbounded.</summary>
    [Parameter]
    public TValue? Max { get; set; }

    /// <summary>Stepping increment; <see langword="null" /> steps by 1.</summary>
    [Parameter]
    public TValue? Step { get; set; }

    /// <summary>
    /// Fractional digits used for display and commit rounding;
    /// <see langword="null" /> derives them from <see cref="Step" />.
    /// </summary>
    [Parameter]
    public int? Precision { get; set; }

    [Parameter]
    public bool ShowControls { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? AutoComplete { get; set; }

    /// <summary>
    /// Virtual keyboard hint; <see langword="null" /> resolves to `numeric` for
    /// integer value types and `decimal` otherwise.
    /// </summary>
    [Parameter]
    public string? InputMode { get; set; }

    /// <summary>
    /// Accessible name of the increment action. Defaults to
    /// <see cref="AeterniUITextOptions.InputNumberIncrementLabel" />.
    /// </summary>
    [Parameter]
    public string? IncrementLabel { get; set; }

    /// <summary>
    /// Accessible name of the decrement action. Defaults to
    /// <see cref="AeterniUITextOptions.InputNumberDecrementLabel" />.
    /// </summary>
    [Parameter]
    public string? DecrementLabel { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string _text = string.Empty;
    private TValue? _lastCommitted;
    private bool _effectiveDisabled;
    private bool _effectiveInvalid;
    private bool _effectiveRequired;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private static readonly bool UsesNumericInputMode = IsIntegralType(typeof(TValue));

    // Explicit type list rather than IBinaryInteger<TSelf>: the interface cannot
    // be mentioned for an open type parameter without re-stating its self
    // constraint, and this is only a keyboard hint for the inner input.
    private static bool IsIntegralType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte)
        || type == typeof(short) || type == typeof(ushort)
        || type == typeof(int) || type == typeof(uint)
        || type == typeof(nint) || type == typeof(nuint)
        || type == typeof(long) || type == typeof(ulong)
        || type == typeof(Int128) || type == typeof(UInt128)
        || type == typeof(BigInteger);

    /// <summary>
    /// The editable element is the inner input, not the composite root, so the
    /// root carries no native disabled capability and only announces its state.
    /// </summary>
    protected override bool SupportsDisabled => false;

    private TValue EffectiveStep => Step ?? TValue.One;

    private int EffectivePrecision => Precision ?? NumericValue.DecimalPlaces(EffectiveStep);

    /// <summary>
    /// Basis for the next step: the last committed value, or the lower bound (or
    /// zero) when the field is empty, so stepping out of an empty field lands on
    /// the grid instead of jumping to an arbitrary place.
    /// </summary>
    private TValue StepBasis => _lastCommitted ?? Min ?? TValue.Zero;

    private bool CanChangeValue => !_effectiveDisabled && !ReadOnly;

    private bool IsInvalid => _effectiveInvalid ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private bool IsDecrementDisabled => !CanChangeValue || (Min is { } min && StepBasis <= min);

    private bool IsIncrementDisabled => !CanChangeValue || (Max is { } max && StepBasis >= max);

    private string EffectiveIncrementLabel => string.IsNullOrWhiteSpace(IncrementLabel)
        ? UiText.InputNumberIncrementLabel
        : IncrementLabel.Trim();

    private string EffectiveDecrementLabel => string.IsNullOrWhiteSpace(DecrementLabel)
        ? UiText.InputNumberDecrementLabel
        : DecrementLabel.Trim();

    private string EffectiveInputMode => string.IsNullOrWhiteSpace(InputMode)
        ? (UsesNumericInputMode ? "numeric" : "decimal")
        : InputMode.Trim();

    /// <summary>
    /// Spinbutton semantics for the inner input: the composite steppers are only
    /// an affordance on top of a value the input itself owns, so the role and the
    /// value bounds belong on the editable element. The attributes are handed to
    /// the child <c>Input</c> through its unmatched-attribute passthrough.
    /// </summary>
    private IReadOnlyDictionary<string, object> SpinButtonAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "spinbutton"
            };

            if (_lastCommitted is { } current)
            {
                attributes["aria-valuenow"] = NumericValue.Format(current, EffectivePrecision, CultureInfo.InvariantCulture);
            }

            if (Min is { } min)
            {
                attributes["aria-valuemin"] = NumericValue.Format(min, EffectivePrecision, CultureInfo.InvariantCulture);
            }

            if (Max is { } max)
            {
                attributes["aria-valuemax"] = NumericValue.Format(max, EffectivePrecision, CultureInfo.InvariantCulture);
            }

            return attributes;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ValidateParameters();

        _effectiveInvalid = Invalid || (FormField?.Invalid ?? false);
        _effectiveDisabled = Disabled || (FormField?.Disabled ?? false);
        _effectiveRequired = Required || (FormField?.Required ?? false);

        var incoming = Value is { } value && !NumericValue.IsFinite(value) ? null : Value;

        // Reformat only when the value changed outside this control: commits
        // record _lastCommitted before invoking ValueChanged, so the echo render
        // after our own change keeps the draft the user is typing.
        if (!NullableEquals(incoming, _lastCommitted))
        {
            _lastCommitted = incoming;
            _text = NumericValue.Format(incoming, EffectivePrecision, CultureInfo.CurrentCulture);
        }

        UpdateEditContextSubscription();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-input-number")
        .Add(SizeClass)
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", ReadOnly)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", _effectiveDisabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);

        if (_effectiveDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("inputnumber", "sync", InstanceId, RootElement);

    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }

    private async Task HandleTextChangedAsync(string? text)
    {
        if (!CanChangeValue)
        {
            return;
        }

        _text = text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_text))
        {
            await CommitAsync(null, reformat: false);
            return;
        }

        if (NumericValue.TryParse<TValue>(_text, CultureInfo.CurrentCulture, out var parsed))
        {
            // Typing updates the value live; clamping and precision rounding wait
            // for blur or a step so the draft is never rewritten mid-keystroke.
            await CommitAsync(parsed, reformat: false);
        }
    }

    private async Task HandleBlurAsync(FocusEventArgs args)
    {
        if (!CanChangeValue)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_text))
        {
            await CommitAsync(null, reformat: true);
        }
        else if (NumericValue.TryParse<TValue>(_text, CultureInfo.CurrentCulture, out var parsed))
        {
            await CommitAsync(ClampAndRound(parsed), reformat: true);
        }
        else
        {
            // Unparseable drafts revert to the last committed value on blur.
            _text = NumericValue.Format(_lastCommitted, EffectivePrecision, CultureInfo.CurrentCulture);
        }
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!CanChangeValue || args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return Task.CompletedTask;
        }

        return args.Key switch
        {
            "ArrowUp" => StepAsync(1),
            "ArrowDown" => StepAsync(-1),
            "PageUp" => StepAsync(1, PageStepMultiplier),
            "PageDown" => StepAsync(-1, PageStepMultiplier),
            _ => Task.CompletedTask
        };
    }

    private Task StepAsync(int direction, int multiplier = 1)
    {
        if (!CanChangeValue)
        {
            return Task.CompletedTask;
        }

        var delta = NumericValue.SaturatingMultiply(EffectiveStep, multiplier * direction);
        return CommitAsync(ClampAndRound(NumericValue.SaturatingAdd(StepBasis, delta)), reformat: true);
    }

    private async Task CommitAsync(TValue? value, bool reformat)
    {
        var changed = !NullableEquals(value, _lastCommitted);
        _lastCommitted = value;

        if (reformat)
        {
            _text = NumericValue.Format(value, EffectivePrecision, CultureInfo.CurrentCulture);
        }

        if (!changed)
        {
            return;
        }

        Value = value;

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        if (_hasFieldIdentifier)
        {
            _validation.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    private TValue ClampAndRound(TValue value)
    {
        // Round first, then clamp: rounding after the clamp could push the result
        // back outside the bound (Max = 0.35 with Precision = 1 turned the clamped
        // 0.35 into 0.4), which then contradicted aria-valuemax and the
        // increment/decrement disabled logic (REV-135).
        value = NumericValue.Round(value, EffectivePrecision);

        if (Min is { } min && value < min)
        {
            value = min;
        }

        if (Max is { } max && value > max)
        {
            value = max;
        }

        return value;
    }

    private void ValidateParameters()
    {
        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown input number size.");
        }

        if (Min is { } min && !NumericValue.IsFinite(min))
        {
            throw new ArgumentOutOfRangeException(nameof(Min), Min, "The minimum must be a finite number.");
        }

        if (Max is { } max && !NumericValue.IsFinite(max))
        {
            throw new ArgumentOutOfRangeException(nameof(Max), Max, "The maximum must be a finite number.");
        }

        if (Min is { } lower && Max is { } upper && lower > upper)
        {
            throw new ArgumentException("The minimum cannot be greater than the maximum.", nameof(Min));
        }

        var step = EffectiveStep;

        if (!NumericValue.IsFinite(step) || step <= TValue.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Step), Step, "The step must be a finite number greater than zero.");
        }

        if (Precision is { } precision)
        {
            if (precision < 0 || precision > NumericValue.MaxPrecision)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Precision),
                    Precision,
                    $"The precision must be between 0 and {NumericValue.MaxPrecision}.");
            }

            // Stepping rounds back onto the precision grid, so a precision below
            // the step's own decimal places would silently pull values off the
            // step grid instead of reporting the configuration mistake.
            if (precision < NumericValue.DecimalPlaces(step))
            {
                throw new ArgumentException(
                    "The precision cannot be lower than the step's decimal places.",
                    nameof(Precision));
            }
        }
    }

    private void UpdateEditContextSubscription()
    {
        _validation.Attach(ValueExpression is null ? null : CascadedEditContext);

        if (ValueExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            _hasFieldIdentifier = true;
        }
        else
        {
            _hasFieldIdentifier = false;
        }
    }

    private string? SizeClass => ComponentClass.ForSize("aeterni-input-number", Size);

    private RenderFragment MinusIcon => builder =>
    {
        builder.OpenComponent<AeterniUI.Components.Icon.Icon>(0);
        builder.AddAttribute(1, nameof(AeterniUI.Components.Icon.Icon.Definition), AeterniIcons.Minus);
        builder.CloseComponent();
    };

    private RenderFragment PlusIcon => builder =>
    {
        builder.OpenComponent<AeterniUI.Components.Icon.Icon>(0);
        builder.AddAttribute(1, nameof(AeterniUI.Components.Icon.Icon.Definition), AeterniIcons.Plus);
        builder.CloseComponent();
    };

    private static bool NullableEquals(TValue? left, TValue? right) =>
        left.HasValue == right.HasValue
        && (!left.HasValue || EqualityComparer<TValue>.Default.Equals(left.GetValueOrDefault(), right.GetValueOrDefault()));
}
