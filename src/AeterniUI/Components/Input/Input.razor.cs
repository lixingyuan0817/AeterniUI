using System.Linq.Expressions;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using AeterniUI.Components.FormField;

namespace AeterniUI.Components.Input;

public partial class Input : AeterniComponent
{
    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    [Parameter]
    public InputType Type { get; set; } = InputType.Text;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? AutoComplete { get; set; }

    [Parameter]
    public string? InputMode { get; set; }

    [Parameter]
    public string? Pattern { get; set; }

    [Parameter]
    public int? MinLength { get; set; }

    [Parameter]
    public int? MaxLength { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public EventCallback<ChangeEventArgs> OnInput { get; set; }

    [Parameter]
    public EventCallback<ChangeEventArgs> OnChange { get; set; }

    [Parameter]
    public EventCallback<FocusEventArgs> OnFocus { get; set; }

    [Parameter]
    public EventCallback<FocusEventArgs> OnBlur { get; set; }

    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string? _currentValue;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private string? _effectiveId;
    private string? _effectiveAriaDescribedBy;
    private bool _effectiveInvalid;
    private bool _effectiveDisabled;
    private bool _effectiveRequired;

    protected override bool SupportsDisabled => true;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ValidateParameters();
        _effectiveId = FormField?.InputId ?? Id;
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
            .Add("aeterni-input")
            .Add(SizeClass)
            .Add("is-full-width", FullWidth)
            .Add("is-readonly", ReadOnly)
            .Add("is-invalid", IsInvalid)
            .Add("is-disabled", _effectiveDisabled);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = TypeClass,
            ["value"] = _currentValue ?? string.Empty
        };

        AddAttribute(attributes, "placeholder", Placeholder);
        AddAttribute(attributes, "name", Name);
        AddAttribute(attributes, "autocomplete", AutoComplete);
        AddAttribute(attributes, "inputmode", InputMode);
        AddAttribute(attributes, "pattern", Pattern);

        if (MinLength.HasValue)
        {
            attributes["minlength"] = MinLength.Value;
        }

        if (MaxLength.HasValue)
        {
            attributes["maxlength"] = MaxLength.Value;
        }

        if (ReadOnly)
        {
            attributes["readonly"] = true;
            attributes["aria-readonly"] = "true";
        }

        if (!string.IsNullOrWhiteSpace(_effectiveId))
        {
            attributes["id"] = _effectiveId.Trim();
        }

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

        return attributes;
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        if (!CanChangeValue)
        {
            return;
        }

        await SetCurrentValueAsync(args.Value?.ToString());
        await OnInput.InvokeAsync(args);
    }

    private async Task HandleChangeAsync(ChangeEventArgs args)
    {
        if (!CanChangeValue)
        {
            return;
        }

        await SetCurrentValueAsync(args.Value?.ToString());
        await OnChange.InvokeAsync(args);
    }

    private Task HandleFocusAsync(FocusEventArgs args) => OnFocus.InvokeAsync(args);

    private Task HandleBlurAsync(FocusEventArgs args) => OnBlur.InvokeAsync(args);

    private Task HandleKeyDownAsync(KeyboardEventArgs args) => OnKeyDown.InvokeAsync(args);

    private async Task SetCurrentValueAsync(string? value)
    {
        if (string.Equals(_currentValue, value, StringComparison.Ordinal))
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

    private bool IsInvalid => _effectiveInvalid ||
        (_hasFieldIdentifier &&
         _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);

    private bool CanChangeValue => !_effectiveDisabled && !ReadOnly;

    private string TypeClass => Type switch
    {
        InputType.Password => "password",
        InputType.Email => "email",
        InputType.Search => "search",
        InputType.Tel => "tel",
        InputType.Url => "url",
        _ => "text"
    };

    private string? SizeClass => ComponentClass.ForSize("aeterni-input", Size);

    private void ValidateParameters()
    {
        if (!Enum.IsDefined(Type))
        {
            throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown input type.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown input size.");
        }

        if (MinLength is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinLength), "The minimum length cannot be negative.");
        }

        if (MaxLength is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxLength), "The maximum length cannot be negative.");
        }

        if (MinLength.HasValue && MaxLength.HasValue && MinLength > MaxLength)
        {
            throw new ArgumentException("The minimum length cannot be greater than the maximum length.", nameof(MinLength));
        }
    }

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
