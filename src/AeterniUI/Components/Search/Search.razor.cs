using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Search;

/// <summary>
/// A keyword search control with explicit submit and clear actions.
/// Search does not own result data or render a suggestion popup; use
/// <c>Autocomplete</c> for input suggestions.
/// </summary>
public partial class Search : AeterniComponent
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public Search() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    [Parameter]
    public EventCallback<string?> OnSearch { get; set; }

    [Parameter]
    public EventCallback OnClear { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public bool Clearable { get; set; } = true;

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public bool SubmitOnEnter { get; set; } = true;

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? AutoComplete { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public string? SubmitLabel { get; set; }

    [Parameter]
    public string? ClearLabel { get; set; }

    private string? _currentValue;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsEffectivelyDisabled => IsDisabled || Loading;

    private bool IsInvalid => Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private bool IsRequired => Required || (FormField?.Required ?? false);

    private bool HasValue => !string.IsNullOrEmpty(_currentValue);

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.SearchLabel
        : AriaLabel.Trim();

    private string EffectiveSubmitLabel => string.IsNullOrWhiteSpace(SubmitLabel)
        ? UiText.SearchSubmitLabel
        : SubmitLabel.Trim();

    private string EffectiveClearLabel => string.IsNullOrWhiteSpace(ClearLabel)
        ? UiText.SearchClearLabel
        : ClearLabel.Trim();

    private string? InputAriaLabel => FormField?.LabelId is null ? EffectiveAriaLabel : AriaLabel;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown search size.");
        }

        _currentValue = Value;
        UpdateEditContextSubscription();
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-search")
        .Add(SizeClass)
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", ReadOnly)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", IsEffectivelyDisabled)
        .Add("is-loading", Loading);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "search",
            ["aria-label"] = EffectiveAriaLabel
        };

        if (IsEffectivelyDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        if (Loading)
        {
            attributes["aria-busy"] = "true";
        }

        return attributes;
    }

    private async Task HandleValueChangedAsync(string? value)
    {
        if (IsEffectivelyDisabled || ReadOnly)
        {
            return;
        }

        _currentValue = value;
        Value = value;
        await ValueChanged.InvokeAsync(value);
        NotifyFieldChanged();
    }

    private Task HandleInputKeyDownAsync(KeyboardEventArgs args) =>
        SubmitOnEnter && !IsEffectivelyDisabled && !ReadOnly && args.Key == "Enter" && !args.AltKey && !args.CtrlKey && !args.MetaKey && !args.ShiftKey
            ? SubmitAsync()
            : Task.CompletedTask;

    private async Task SubmitAsync()
    {
        if (IsEffectivelyDisabled)
        {
            return;
        }

        await OnSearch.InvokeAsync(_currentValue);
    }

    private async Task ClearAsync(MouseEventArgs _)
    {
        if (IsEffectivelyDisabled || ReadOnly || !HasValue)
        {
            return;
        }

        _currentValue = string.Empty;
        Value = _currentValue;
        await ValueChanged.InvokeAsync(_currentValue);
        NotifyFieldChanged();
        await OnClear.InvokeAsync();
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


    private void UnsubscribeFromEditContext() => _validation.Detach();


    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier)
        {
            _validation.NotifyFieldChanged(_fieldIdentifier);
        }
    }




    private string? SizeClass => ComponentClass.ForSize("aeterni-search", Size);

    private RenderFragment ClearIcon => builder =>
    {
        builder.OpenComponent<AeterniUI.Components.Icon.Icon>(0);
        builder.AddAttribute(1, nameof(AeterniUI.Components.Icon.Icon.Definition), AeterniIcons.Xmark);
        builder.CloseComponent();
    };

    private RenderFragment SearchIcon => builder =>
    {
        builder.OpenComponent<AeterniUI.Components.Icon.Icon>(0);
        builder.AddAttribute(1, nameof(AeterniUI.Components.Icon.Icon.Definition), AeterniIcons.MagnifyingGlass);
        builder.CloseComponent();
    };
}
