using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Switch;

public partial class Switch : AeterniComponent
{
    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [Parameter]
    public bool Value { get; set; }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<bool>>? ValueExpression { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public EventCallback<ChangeEventArgs> OnChange { get; set; }

    private ElementReference _inputElement;
    private bool _currentValue;
    private string? _effectiveId;
    private string? _effectiveAriaDescribedBy;
    private bool _effectiveInvalid;
    private bool _effectiveDisabled;
    private bool _effectiveRequired;

    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _effectiveId = FormField?.InputId ?? $"{ElementId}-input";
        _effectiveAriaDescribedBy = string.Join(" ", new[] { AriaDescribedBy, FormField?.DescribedBy }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        _effectiveInvalid = Invalid || (FormField?.Invalid ?? false);
        _effectiveDisabled = Disabled || (FormField?.Disabled ?? false);
        _effectiveRequired = Required || (FormField?.Required ?? false);
        _currentValue = Value;
        UpdateEditContextSubscription();
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-switch")
            .Add("is-checked", _currentValue)
            .Add("is-invalid", IsInvalid)
            .Add("is-disabled", _effectiveDisabled)
;
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (_effectiveDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private IReadOnlyDictionary<string, object> BuildInputAttributes()
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (_effectiveDisabled)
        {
            attributes["disabled"] = true;
            attributes["aria-disabled"] = "true";
        }

        if (_effectiveRequired)
        {
            attributes["required"] = true;
            attributes["aria-required"] = "true";
        }

        if (IsInvalid)
        {
            attributes["aria-invalid"] = "true";
        }

        AddAttribute(attributes, "aria-label", AriaLabel);
        AddAttribute(attributes, "aria-describedby", _effectiveAriaDescribedBy);
        AddAttribute(attributes, "name", Name);

        return attributes;
    }

    private async Task HandleChangeAsync(ChangeEventArgs args)
    {
        if (_effectiveDisabled)
        {
            return;
        }

        var nextValue = !_currentValue;
        await SetValueAsync(nextValue);
        await OnChange.InvokeAsync(args);
    }

    private async Task SetValueAsync(bool value)
    {
        if (_currentValue == value && Value == value)
        {
            return;
        }

        _currentValue = value;
        Value = value;

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        if (_hasFieldIdentifier && _subscribedEditContext is not null)
        {
            _subscribedEditContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }

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

    protected override ValueTask OnComponentDisposeAsync()
    {
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    private bool IsInvalid =>
        _effectiveInvalid ||
        (_hasFieldIdentifier &&
         _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);

    private static void AddAttribute(
        IDictionary<string, object> attributes,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes[name] = value.Trim();
        }
    }
}
