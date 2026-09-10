using System.Linq.Expressions;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.ComboBox;

/// <summary>
/// A plain dropdown selector. Clicking the trigger opens the option list;
/// selecting (or clicking outside / pressing Escape) closes it. No free-text
/// search is performed — the trigger behaves like a read-only select.
/// </summary>
[JsModule("Components/ComboBox/ComboBox.razor.js", Name = "combobox", Interactive = true)]
public partial class ComboBox<TItem> : AeterniComponent where TItem : class
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [Parameter]
    public TItem? Value { get; set; }

    [Parameter]
    public EventCallback<TItem?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<TItem?>>? ValueExpression { get; set; }

    [Parameter]
    public IReadOnlyList<TItem>? Items { get; set; }

    [Parameter]
    public Func<TItem, string>? TextSelector { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// Control tier. Matches Input/Textarea tiers so a small form row lines up:
    /// <see cref="Size.Small" /> uses the same height as <c>Input Size="Small"</c>.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public EventCallback<TItem?> OnChange { get; set; }

    private ElementReference _triggerElement;
    private bool _open;
    private bool _positionListenersActive;
    private int _activeIndex = -1;
    private readonly List<TItem> _visibleItems = [];

    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private string ListboxId => $"{ElementId}-list";

    // The trigger adopts the FormField input id so the field label's `for` still
    // resolves; the field label itself is linked through aria-labelledby because
    // a select-style trigger is a button, not a labelable text control.
    private string? FormFieldInputId => FormField?.InputId;
    private string? FormFieldLabelId => FormField?.LabelId;
    private string? FormFieldDescribedBy => FormField?.DescribedBy;

    private string PlaceholderText => string.IsNullOrWhiteSpace(Placeholder) ? UiText.ComboBoxPlaceholder : Placeholder;

    private string ListLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.ComboBoxListLabel : AriaLabel;

    private bool IsInvalid =>
        Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier &&
         _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();
        RebuildVisibleItems();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown combo box size.");
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-combobox")
            .Add(SizeClass)
            .Add("is-open", _open)
            .Add("is-invalid", IsInvalid)
            .Add("is-disabled", Disabled);
    }

    private string? SizeClass => ComponentClass.ForSize("aeterni-combobox", Size);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (Disabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private async Task HandleTriggerClickAsync(MouseEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        if (_open)
        {
            await CloseAsync();
        }
        else
        {
            await OpenAsync();
        }
    }

    private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                if (!_open)
                {
                    await OpenAsync();
                }
                else
                {
                    MoveActive(1);
                    await InvokeAsync(StateHasChanged);
                }

                break;

            case "ArrowUp":
                if (_open)
                {
                    MoveActive(-1);
                    await InvokeAsync(StateHasChanged);
                }

                break;

            case "Home":
                if (_open)
                {
                    _activeIndex = _visibleItems.Count > 0 ? 0 : -1;
                    await InvokeAsync(StateHasChanged);
                }

                break;

            case "End":
                if (_open)
                {
                    _activeIndex = _visibleItems.Count > 0 ? _visibleItems.Count - 1 : -1;
                    await InvokeAsync(StateHasChanged);
                }

                break;

            case "Enter":
                if (_open && _activeIndex >= 0 && _activeIndex < _visibleItems.Count)
                {
                    await ChooseAsync(_visibleItems[_activeIndex]);
                }

                break;

            case "Escape":
                if (_open)
                {
                    await CloseAsync();
                }

                break;
        }
    }

    private Task OpenAsync()
    {
        if (_visibleItems.Count == 0)
        {
            return Task.CompletedTask;
        }

        _open = true;
        _activeIndex = Math.Max(0, _visibleItems.FindIndex(item => ValuesEqual(item, Value)));
        return InvokeAsync(StateHasChanged);
    }

    private async Task CloseAsync()
    {
        _open = false;
        _activeIndex = -1;
        await InvokeAsync(StateHasChanged);
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

    private async Task ChooseAsync(TItem item)
    {
        Value = item;
        await ValueChanged.InvokeAsync(item);
        await OnChange.InvokeAsync(item);
        NotifyFieldChanged();
        _open = false;
        _activeIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    private void RebuildVisibleItems()
    {
        _visibleItems.Clear();
        if (Items is not null)
        {
            _visibleItems.AddRange(Items);
        }
    }

    private string DisplayText(TItem? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (TextSelector is not null)
        {
            return TextSelector(item) ?? string.Empty;
        }

        return item.ToString() ?? string.Empty;
    }

    private static bool ValuesEqual(TItem? left, TItem? right) =>
        EqualityComparer<TItem>.Default.Equals(left!, right!);

    [JSInvokable]
    public Task OnOutsidePointerAsync() => CloseAsync();

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                await JsModuleManager.InvokeModuleVoidAsync(
                    "combobox",
                    "attach",
                    InstanceId,
                    RootElement);
            }

            // While the dropdown is open, the JS module closes it as soon as
            // the page scrolls (native select behaviour) and repositions on
            // resize. Track the listener lifecycle here and keep the popover
            // anchored to the trigger after each re-render.
            if (_open && !_positionListenersActive)
            {
                await JsModuleManager.InvokeModuleVoidAsync(
                    "combobox",
                    "setOpen",
                    InstanceId,
                    true);
                _positionListenersActive = true;
            }
            else if (!_open && _positionListenersActive)
            {
                await JsModuleManager.InvokeModuleVoidAsync(
                    "combobox",
                    "setOpen",
                    InstanceId,
                    false);
                _positionListenersActive = false;
            }

            if (_open)
            {
                await JsModuleManager.InvokeModuleVoidAsync(
                    "combobox",
                    "place",
                    InstanceId);
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _open = false;
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
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

    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier && _subscribedEditContext is not null)
        {
            _subscribedEditContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }
}
