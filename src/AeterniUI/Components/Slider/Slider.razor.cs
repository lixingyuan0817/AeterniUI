using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Slider;

/// <summary>
/// A single-value slider. The root element is the slider itself — the one
/// focusable node carrying <c>role="slider"</c> and the value semantics — and
/// everything painted below it is decorative.
/// </summary>
[JsModule("Components/Slider/Slider.razor.js", Name = "slider", Interactive = true)]
public partial class Slider<TValue> : AeterniComponent
    where TValue : struct, INumber<TValue>
{
    /// <summary>PageUp/PageDown step this many increments at once.</summary>
    private const int PageStepMultiplier = 10;

    /// <summary>Continuous sliders move one percent of their range per arrow key.</summary>
    private const int ContinuousKeyboardDivisions = 100;

    /// <summary>Continuous fractional values stay readable without hiding useful precision.</summary>
    private const int MinimumContinuousPrecision = 2;

    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public Slider() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public TValue Value { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>Lower bound; <see langword="null" /> starts at zero.</summary>
    [Parameter]
    public TValue? Min { get; set; }

    /// <summary>Upper bound; <see langword="null" /> ends at 100.</summary>
    [Parameter]
    public TValue? Max { get; set; }

    /// <summary>
    /// Optional stepping increment. A positive value snaps pointer and keyboard
    /// input to that grid; <see langword="null" /> enables continuous pointer input.
    /// </summary>
    [Parameter]
    public TValue? Step { get; set; }

    /// <summary>
    /// Fractional digits used for the ARIA value text;
    /// <see langword="null" /> derives them from <see cref="Step" /> or the
    /// continuous keyboard increment.
    /// </summary>
    [Parameter]
    public int? Precision { get; set; }

    /// <summary>Track direction; the value always increases away from the origin corner.</summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    /// <summary>
    /// Accessible name used when no <c>FormField</c> label is in scope. Defaults
    /// to <see cref="AeterniUITextOptions.SliderLabel" />.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool _effectiveDisabled;
    private bool _effectiveInvalid;
    private bool _isRightToLeft;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private TValue Minimum => Min ?? TValue.Zero;

    private TValue Maximum => Max ?? TValue.CreateChecked(100);

    private static bool SupportsFractionalValues =>
        TValue.One / TValue.CreateChecked(2) != TValue.Zero;

    private TValue KeyboardStep
    {
        get
        {
            if (Step is { } step)
            {
                return step;
            }

            var span = NumericValue.ToDouble(Maximum - Minimum);
            var increment = NumericValue.FromDouble<TValue>(span / ContinuousKeyboardDivisions);
            return NumericValue.IsFinite(increment) && increment > TValue.Zero
                ? increment
                : TValue.One;
        }
    }

    private int EffectivePrecision => Precision ?? (Step is { } step
        ? NumericValue.DecimalPlaces(step)
        : SupportsFractionalValues
            ? Math.Max(MinimumContinuousPrecision, NumericValue.DecimalPlaces(KeyboardStep))
            : 0);

    private bool CanInteract => !_effectiveDisabled && Visible;

    private bool IsInvalid => _effectiveInvalid ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    /// <summary>
    /// The clamped display value. Values outside the range or outside the number
    /// system (NaN, infinity) are clamped rather than thrown: a binding race must
    /// never tear down the render, and the parameter itself is left untouched.
    /// </summary>
    private TValue EffectiveValue
    {
        get
        {
            var value = Value;

            if (!NumericValue.IsFinite(value) || value < Minimum)
            {
                return Minimum;
            }

            return value > Maximum ? Maximum : value;
        }
    }

    // A container-style field control: it adopts the field input id so the
    // label's `for` stays resolvable, and is named through aria-labelledby
    // because a div cannot be labelled by `for` alone.
    protected override string ComputeElementId() => FormField?.InputId ?? base.ComputeElementId();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ValidateParameters();

        _effectiveDisabled = Disabled || (FormField?.Disabled ?? false);
        _effectiveInvalid = Invalid || (FormField?.Invalid ?? false);

        UpdateEditContextSubscription();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-slider")
        .Add(SizeClass)
        .Add("aeterni-slider--vertical", Orientation == Orientation.Vertical)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", _effectiveDisabled);

    protected override StyleBuilder BuildStyle() =>
        base.BuildStyle().Add($"--aeterni-slider-value: {FractionText(Fraction(EffectiveValue))}");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "slider",
            ["aria-valuemin"] = NumericValue.Format(Minimum, EffectivePrecision, CultureInfo.InvariantCulture),
            ["aria-valuemax"] = NumericValue.Format(Maximum, EffectivePrecision, CultureInfo.InvariantCulture),
            ["aria-valuenow"] = NumericValue.Format(EffectiveValue, EffectivePrecision, CultureInfo.InvariantCulture),
            ["aria-valuetext"] = NumericValue.Format(EffectiveValue, EffectivePrecision, CultureInfo.CurrentCulture)
        };

        if (!_effectiveDisabled)
        {
            attributes["tabindex"] = "0";
        }

        if (Orientation == Orientation.Vertical)
        {
            attributes["aria-orientation"] = "vertical";
        }

        if (FormField?.LabelId is { } labelId)
        {
            attributes["aria-labelledby"] = labelId;
        }
        else
        {
            attributes["aria-label"] = string.IsNullOrWhiteSpace(AriaLabel) ? UiText.SliderLabel : AriaLabel.Trim();
        }

        var describedBy = string.Join(" ", new[] { AriaDescribedBy, FormField?.DescribedBy }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (describedBy.Length > 0)
        {
            attributes["aria-describedby"] = describedBy;
        }

        if (_effectiveDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        if (IsInvalid)
        {
            attributes["aria-invalid"] = "true";
        }

        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync(
            "slider",
            "sync",
            InstanceId,
            RootElement,
            CanInteract,
            NumericValue.ToDouble(Minimum),
            NumericValue.ToDouble(Maximum),
            Step is { } step ? NumericValue.ToDouble(step) : 0d,
            Orientation == Orientation.Vertical ? "vertical" : "horizontal");

    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Commits a pointer fraction reported by the drag module. The fraction is
    /// geometry, not a value: all value arithmetic stays on <typeparamref name="TValue" />
    /// so decimal and 64-bit ranges never round-trip through a double.
    /// </summary>
    [JSInvokable]
    public async Task HandlePointerFractionAsync(double fraction)
    {
        if (IsDisposed || !CanInteract || !double.IsFinite(fraction))
        {
            return;
        }

        var clamped = Math.Clamp(fraction, 0d, 1d);

        // Only the pointer geometry passes through a double: the fraction is
        // scaled against the span first, then converted once, because converting
        // the fraction itself would truncate to zero for integer value types.
        var span = NumericValue.ToDouble(Maximum - Minimum);
        var next = Minimum + NumericValue.FromDouble<TValue>(span * clamped);

        await SetValueAsync(SnapIfNeeded(next));
        RequestStateHasChanged();
    }

    /// <summary>
    /// Records the page's reading direction (reported by the drag module when it
    /// changes) so ArrowLeft/ArrowRight step toward the direction the track
    /// visually grows in. Nothing rendered depends on this, so no re-render is
    /// requested.
    /// </summary>
    [JSInvokable]
    public Task HandleDirectionAsync(bool isRightToLeft)
    {
        if (!IsDisposed)
        {
            _isRightToLeft = isRightToLeft;
        }

        return Task.CompletedTask;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!CanInteract || args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return;
        }

        var step = KeyboardStep;
        var pageStep = step * TValue.CreateChecked(PageStepMultiplier);

        TValue? next = args.Key switch
        {
            "ArrowUp" => StepFrom(step),
            "ArrowDown" => StepFrom(-step),
            // Left/right apply to the horizontal track only, and follow the
            // direction it visually grows in (APG: vertical sliders use up/down).
            "ArrowRight" when Orientation == Orientation.Horizontal => StepFrom(_isRightToLeft ? -step : step),
            "ArrowLeft" when Orientation == Orientation.Horizontal => StepFrom(_isRightToLeft ? step : -step),
            "PageUp" => StepFrom(pageStep),
            "PageDown" => StepFrom(-pageStep),
            "Home" => Minimum,
            "End" => Maximum,
            _ => null
        };

        if (next is { } value)
        {
            await SetValueAsync(value);
        }
    }

    private TValue StepFrom(TValue delta)
    {
        var next = EffectiveValue + delta;

        if (next < Minimum)
        {
            next = Minimum;
        }
        else if (next > Maximum)
        {
            next = Maximum;
        }

        return SnapIfNeeded(next);
    }

    private TValue SnapIfNeeded(TValue value) =>
        Step is { } step ? NumericValue.SnapToStep(value, Minimum, step) : value;

    private async Task SetValueAsync(TValue value)
    {
        if (EqualityComparer<TValue>.Default.Equals(Value, value))
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

    /// <summary>Position of a value on the track, 0 at the minimum and 1 at the maximum.</summary>
    private double Fraction(TValue value)
    {
        var minimum = NumericValue.ToDouble(Minimum);
        var span = NumericValue.ToDouble(Maximum) - minimum;

        if (!double.IsFinite(span) || span <= 0)
        {
            return 0;
        }

        var fraction = (NumericValue.ToDouble(value) - minimum) / span;
        return double.IsFinite(fraction) ? Math.Clamp(fraction, 0d, 1d) : 0;
    }

    private static string FractionText(double fraction) =>
        fraction.ToString("0.####", CultureInfo.InvariantCulture);

    private void ValidateParameters()
    {
        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown slider size.");
        }

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown slider orientation.");
        }

        if (Min is { } min && !NumericValue.IsFinite(min))
        {
            throw new ArgumentOutOfRangeException(nameof(Min), Min, "The minimum must be a finite number.");
        }

        if (Max is { } max && !NumericValue.IsFinite(max))
        {
            throw new ArgumentOutOfRangeException(nameof(Max), Max, "The maximum must be a finite number.");
        }

        if (Minimum >= Maximum)
        {
            throw new ArgumentException("The minimum must be smaller than the maximum.", nameof(Min));
        }

        if (Step is { } step && (!NumericValue.IsFinite(step) || step <= TValue.Zero))
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

            if (Step is { } configuredStep && precision < NumericValue.DecimalPlaces(configuredStep))
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

    private string? SizeClass => ComponentClass.ForSize("aeterni-slider", Size);
}
