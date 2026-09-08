using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Components.FormField;
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

    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = "Rating";

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
            .Add("is-readonly", ReadOnly)
            .Add("is-invalid", IsInvalid)
            .Add("is-disabled", Disabled);
    }

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
