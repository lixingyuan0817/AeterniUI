using System.Linq.Expressions;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Autocomplete;

/// <summary>
/// A free-text input with a host-provided suggestion list. The component owns
/// presentation and keyboard semantics; it does not fetch, cache or filter data.
/// </summary>
[JsModule("Components/Autocomplete/Autocomplete.razor.js", Name = "autocomplete", Interactive = true)]
public partial class Autocomplete<TItem> : AeterniComponent where TItem : class
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public Autocomplete() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    [Parameter]
    public IReadOnlyList<TItem>? Items { get; set; }

    [Parameter]
    public Func<TItem, string>? TextSelector { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public EventCallback<TItem> OnItemSelected { get; set; }

    [Parameter]
    public EventCallback OnClear { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public bool Clearable { get; set; } = true;

    /// <summary>
    /// Host is refreshing the suggestion snapshot. The suggestion region shows a
    /// busy status and the root reports <c>aria-busy</c>; the input, the clear
    /// action and opening/closing the panel stay usable so the host can keep the
    /// field interactive while it waits for data.
    /// </summary>
    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public bool OpenOnFocus { get; set; } = true;

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Preferred side of the suggestion layer relative to the field. Space-aware
    /// flipping and viewport clamping stay owned by the shared floating module.
    /// </summary>
    [Parameter]
    public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;

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
    public string? ClearLabel { get; set; }

    private readonly List<TItem> _visibleItems = [];
    private string? _currentValue;
    private bool _open;
    private int _activeIndex = -1;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private ElementReference _inputElement;

    private string ListboxId => $"{ElementId}-list";

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsInvalid => Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private bool IsRequired => Required || (FormField?.Required ?? false);

    private bool HasValue => !string.IsNullOrEmpty(_currentValue);

    private bool HasItemsSource => Items is not null;

    // Loading never closes the field: it only swaps the suggestion body for the
    // busy status, so the panel has to be openable while the host refreshes Items
    // even when the current snapshot is empty.
    private bool CanOpen => !IsDisabled && !ReadOnly && (HasItemsSource || Loading || EmptyContent is not null);

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.AutocompleteLabel
        : AriaLabel.Trim();

    private string EffectiveListLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.AutocompleteListLabel
        : AriaLabel.Trim();

    private string EffectiveClearLabel => string.IsNullOrWhiteSpace(ClearLabel)
        ? UiText.SearchClearLabel
        : ClearLabel.Trim();

    private string? InputAriaLabel => FormField?.LabelId is null ? EffectiveAriaLabel : AriaLabel;

    // While the panel shows the loading status no option is rendered, so the
    // trigger must not keep pointing aria-activedescendant at a missing id.
    private string? ActiveOptionId => !Loading && _open && _activeIndex >= 0 && _activeIndex < _visibleItems.Count
        ? $"{ListboxId}-option-{_activeIndex}"
        : null;

    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "combobox",
                ["aria-expanded"] = _open ? "true" : "false",
                ["aria-controls"] = ListboxId,
                ["aria-haspopup"] = "listbox",
                ["aria-autocomplete"] = "list"
            };

            if (ActiveOptionId is not null)
            {
                attributes["aria-activedescendant"] = ActiveOptionId;
            }

            return attributes;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown autocomplete size.");
        }

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown autocomplete placement.");
        }

        _currentValue = Value;
        RebuildItems();
        UpdateEditContextSubscription();

        if (!CanOpen)
        {
            _open = false;
            _activeIndex = -1;
        }
        else if (_open)
        {
            _activeIndex = _visibleItems.Count == 0 ? -1 : Math.Clamp(_activeIndex, 0, _visibleItems.Count - 1);
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _open = false;
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Captures the real input element exposed by the child <c>Input</c>. The
    /// module needs that element, not the component root: a consumer-supplied
    /// <c>Element</c> parameter replaces <c>RootElement</c>, and a querySelector
    /// would then silently find nothing and drop keyboard suppression.
    /// </summary>
    private void HandleInputElementChanged(ElementReference element) => _inputElement = element;

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("autocomplete", "sync", InstanceId, _inputElement);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-autocomplete")
        .Add(SizeClass)
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", ReadOnly)
        .Add("is-open", _open)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", IsDisabled)
        .Add("is-loading", Loading);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (IsDisabled)
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
        if (IsDisabled || ReadOnly)
        {
            return;
        }

        _currentValue = value;
        Value = value;
        _activeIndex = -1;
        await ValueChanged.InvokeAsync(value);
        NotifyFieldChanged();

        if (CanOpen)
        {
            _open = true;
        }

        await InvokeAsync(StateHasChanged);
    }

    private Task HandleFocusAsync(FocusEventArgs _)
    {
        if (OpenOnFocus && CanOpen)
        {
            _open = true;
            _activeIndex = _visibleItems.Count == 0 ? -1 : 0;
        }

        return Task.CompletedTask;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsDisabled || ReadOnly || args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return;
        }

        if (Loading)
        {
            // The body only shows the busy status: the panel can still open and
            // close, but there is no rendered option to move onto or confirm.
            switch (args.Key)
            {
                case "ArrowDown":
                case "ArrowUp":
                    if (!_open)
                    {
                        Open();
                    }

                    await InvokeAsync(StateHasChanged);
                    break;
                case "Escape":
                    if (_open)
                    {
                        Close();
                        await InvokeAsync(StateHasChanged);
                    }

                    break;
            }

            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                if (!_open)
                {
                    Open();
                }
                else
                {
                    MoveActive(1);
                }

                await InvokeAsync(StateHasChanged);
                break;

            case "ArrowUp":
                if (!_open)
                {
                    Open();
                }
                else
                {
                    MoveActive(-1);
                }

                await InvokeAsync(StateHasChanged);
                break;

            case "Home":
                if (_open)
                {
                    _activeIndex = _visibleItems.Count == 0 ? -1 : 0;
                    await InvokeAsync(StateHasChanged);
                }

                break;

            case "End":
                if (_open)
                {
                    _activeIndex = _visibleItems.Count == 0 ? -1 : _visibleItems.Count - 1;
                    await InvokeAsync(StateHasChanged);
                }

                break;

            case "Enter":
                if (_open && _activeIndex >= 0 && _activeIndex < _visibleItems.Count)
                {
                    await SelectAsync(_visibleItems[_activeIndex]);
                }

                break;

            case "Escape":
                if (_open)
                {
                    Close();
                    await InvokeAsync(StateHasChanged);
                }

                break;
        }
    }

    private async Task HandlePopoverOpenChangedAsync(bool open)
    {
        if (open && CanOpen)
        {
            Open();
        }
        else
        {
            Close();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectAsync(TItem item)
    {
        if (IsDisabled || ReadOnly)
        {
            return;
        }

        var text = DisplayText(item);
        _currentValue = text;
        Value = text;
        await ValueChanged.InvokeAsync(text);
        NotifyFieldChanged();
        await OnItemSelected.InvokeAsync(item);
        Close();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearAsync(MouseEventArgs _)
    {
        if (IsDisabled || ReadOnly || !HasValue)
        {
            return;
        }

        _currentValue = string.Empty;
        Value = _currentValue;
        await ValueChanged.InvokeAsync(_currentValue);
        NotifyFieldChanged();
        await OnClear.InvokeAsync();
        Close();
        await InvokeAsync(StateHasChanged);
    }

    private void Open()
    {
        if (!CanOpen)
        {
            return;
        }

        _open = true;
        _activeIndex = _visibleItems.Count == 0 ? -1 : Math.Clamp(_activeIndex < 0 ? 0 : _activeIndex, 0, _visibleItems.Count - 1);
    }

    private void Close()
    {
        _open = false;
        _activeIndex = -1;
    }

    private void MoveActive(int direction)
    {
        if (_visibleItems.Count == 0)
        {
            _activeIndex = -1;
            return;
        }

        _activeIndex = (_activeIndex + direction + _visibleItems.Count) % _visibleItems.Count;
    }

    private void RebuildItems()
    {
        _visibleItems.Clear();
        if (Items is not null)
        {
            _visibleItems.AddRange(Items);
        }
    }

    private string DisplayText(TItem item) => TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty;

    private string? SizeClass => ComponentClass.ForSize("aeterni-autocomplete", Size);

    private string EmptyText => UiText.AutocompleteNoResultsText;

    private RenderFragment ClearIcon => builder =>
    {
        builder.OpenComponent<AeterniUI.Components.Icon.Icon>(0);
        builder.AddAttribute(1, nameof(AeterniUI.Components.Icon.Icon.Definition), AeterniIcons.Xmark);
        builder.CloseComponent();
    };

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



}
