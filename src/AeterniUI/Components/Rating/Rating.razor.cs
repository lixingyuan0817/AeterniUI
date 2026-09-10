using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Rating;

public partial class Rating : AeterniComponent
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<int>>? ValueExpression { get; set; }

    [Parameter]
    public int Max { get; set; } = 5;

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool AllowClear { get; set; }

    /// <summary>
    /// Control tier for the star box and hit target.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Accessible name of the rating group. Defaults to
    /// <see cref="AeterniUITextOptions.RatingLabel"/>.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<int> OnChange { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();

        if (Max <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Max), Max, "The rating maximum must be greater than zero.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown rating size.");
        }

        // Transient out-of-range values (rapid keyboard input, binding races)
        // must never tear down the whole render tree: clamp instead of throw.
        var clamped = Math.Clamp(Value, 0, Max);
        if (clamped != Value)
        {
            Value = clamped;
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-rating")
            .Add(SizeClass)
            .Add("is-readonly", ReadOnly)
            .Add("is-invalid", IsInvalid)
            .Add("is-disabled", Disabled);
    }

    private string? SizeClass => ComponentClass.ForSize("aeterni-rating", Size);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        // A radiogroup container cannot be named by a `for` attribute, so the
        // field label is linked through aria-labelledby; the container still
        // adopts the field input id to keep the label's `for` resolvable.
        if (FormField?.InputId is { } inputId)
        {
            attributes["id"] = inputId;
        }

        if (FormField?.LabelId is { } labelId)
        {
            attributes["aria-labelledby"] = labelId;
        }

        if (FormField?.DescribedBy is { } describedBy)
        {
            attributes["aria-describedby"] = describedBy;
        }

        return attributes;
    }

    /// <summary>
    /// Roving tabindex stop: the selected star (or the first one when nothing is
    /// selected) is the only radio in the Tab sequence, so the group is entered
    /// and left with a single Tab press.
    /// </summary>
    private int TabStop => Value > 0 ? Math.Clamp(Value, 1, Max) : 1;

    /// <summary>
    /// Read-only and disabled ratings are display-only, so the current value is
    /// folded into the group name instead of relying on focusable radios.
    /// </summary>
    private string EffectiveAriaLabel
    {
        get
        {
            var label = string.IsNullOrWhiteSpace(AriaLabel) ? UiText.RatingLabel : AriaLabel;
            return IsInteractive ? label : $"{label}: {ValueLabel}";
        }
    }

    private bool IsInteractive => !ReadOnly && !Disabled;

    private async Task HandleClickAsync(int index)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        if (Value == index && !AllowClear)
        {
            return;
        }

        var nextValue = AllowClear && Value == index ? 0 : index;
        await SetValueAsync(nextValue);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || ReadOnly)
        {
            return;
        }

        var nextValue = args.Key switch
        {
            "ArrowRight" or "ArrowUp" => Math.Min(Value + 1, Max),
            "ArrowLeft" or "ArrowDown" => Math.Max(Value - 1, 0),
            "Home" => 1,
            "End" => Max,
            _ => Value
        };

        if (nextValue != Value)
        {
            await SetValueAsync(nextValue);
        }
    }

    private async Task SetValueAsync(int value)
    {
        if (Value == value)
        {
            return;
        }

        Value = value;
        await ValueChanged.InvokeAsync(value);
        await OnChange.InvokeAsync(value);
        NotifyFieldChanged();
    }

    private bool IsInvalid =>
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier &&
         _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);

    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private void UpdateEditContextSubscription()
    {
        var nextEditContext = ValueExpression is null ? null : CascadedEditContext;
        if (!ReferenceEquals(_subscribedEditContext, nextEditContext))
        {
            UnsubscribeFromEditContext();
            _subscribedEditContext = nextEditContext;
            if (_subscribedEditContext is not null)
            {
                _subscribedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
            }
        }

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

    private void UnsubscribeFromEditContext()
    {
        if (_subscribedEditContext is not null)
        {
            _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
            _subscribedEditContext = null;
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        if (!IsDisposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier && _subscribedEditContext is not null)
        {
            _subscribedEditContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    private string ValueLabel => $"{Value.ToString(CultureInfo.InvariantCulture)} / {Max.ToString(CultureInfo.InvariantCulture)}";
}
