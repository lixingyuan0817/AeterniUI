using System.Linq.Expressions;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.MultiSelect;

/// <summary>
/// A controlled multi-value selector with removable selected items. Items and
/// asynchronous data remain owned by the host; this component owns the trigger,
/// listbox and keyboard interaction.
/// </summary>
[JsModule("Components/MultiSelect/MultiSelect.razor.js", Name = "multi-select", Interactive = true)]
public partial class MultiSelect<TItem> : AeterniComponent where TItem : class
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    [Parameter]
    public IReadOnlyList<TItem> SelectedValues { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> SelectedValuesChanged { get; set; }

    [Parameter]
    public Expression<Func<IReadOnlyList<TItem>>>? SelectedValuesExpression { get; set; }

    [Parameter]
    public Func<TItem, string>? TextSelector { get; set; }

    [Parameter]
    public Func<TItem, bool>? DisabledSelector { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter]
    public RenderFragment<TItem>? SelectedItemTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public EventCallback<TItem> OnItemSelected { get; set; }

    [Parameter]
    public EventCallback OnClear { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public bool AllowSelectAll { get; set; }

    [Parameter]
    public bool AllowClear { get; set; }

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
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public string? SelectAllLabel { get; set; }

    [Parameter]
    public string? ClearAllLabel { get; set; }

    [Parameter]
    public string? RemoveLabel { get; set; }

    private readonly List<TItem> _visibleItems = [];
    private bool _open;
    private int _activeIndex = -1;
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private string ListboxId => $"{ElementId}-list";

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsInvalid => Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _subscribedEditContext?.GetValidationMessages(_fieldIdentifier).Any() == true);

    private bool IsRequired => Required || (FormField?.Required ?? false);

    private bool HasSelection => SelectedValues.Count > 0;

    private bool HasEnabledItems => _visibleItems.Any(item => !IsItemDisabled(item));

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.MultiSelectLabel
        : AriaLabel.Trim();

    private string EffectiveListLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.MultiSelectListLabel
        : AriaLabel.Trim();

    private string PlaceholderText => string.IsNullOrWhiteSpace(Placeholder)
        ? UiText.MultiSelectPlaceholder
        : Placeholder;

    private string EffectiveSelectAllLabel => string.IsNullOrWhiteSpace(SelectAllLabel)
        ? UiText.MultiSelectSelectAllLabel
        : SelectAllLabel.Trim();

    private string EffectiveClearAllLabel => string.IsNullOrWhiteSpace(ClearAllLabel)
        ? UiText.MultiSelectClearAllLabel
        : ClearAllLabel.Trim();

    private string EffectiveRemoveLabel => string.IsNullOrWhiteSpace(RemoveLabel)
        ? UiText.MultiSelectRemoveLabel
        : RemoveLabel.Trim();

    private string? ActiveOptionId => _open && _activeIndex >= 0 && _activeIndex < _visibleItems.Count
        ? $"{ListboxId}-option-{_activeIndex}"
        : null;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown multi-select size.");
        }

        ArgumentNullException.ThrowIfNull(Items);
        ArgumentNullException.ThrowIfNull(SelectedValues);
        RebuildItems();
        UpdateEditContextSubscription();

        if (IsDisabled || !Visible || ReadOnly)
        {
            _open = false;
            _activeIndex = -1;
        }
        else if (_open)
        {
            _activeIndex = _visibleItems.Count == 0
                ? -1
                : _activeIndex >= 0 && _activeIndex < _visibleItems.Count && !IsItemDisabled(_visibleItems[_activeIndex])
                    ? _activeIndex
                    : FindEnabledIndex(0, 1);
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _open = false;
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("multi-select", "sync", InstanceId, RootElement);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-multi-select")
        .Add(SizeClass)
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", ReadOnly)
        .Add("is-open", _open)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", IsDisabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (IsDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private async Task HandleTriggerClickAsync(MouseEventArgs _)
    {
        if (IsDisabled || ReadOnly)
        {
            return;
        }

        if (_open)
        {
            Close();
        }
        else
        {
            Open();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsDisabled || ReadOnly || args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                if (!_open) Open(); else MoveActive(1);
                await InvokeAsync(StateHasChanged);
                break;
            case "ArrowUp":
                if (!_open) Open(); else MoveActive(-1);
                await InvokeAsync(StateHasChanged);
                break;
            case "Home":
                if (_open)
                {
                    _activeIndex = FindEnabledIndex(0, 1);
                    await InvokeAsync(StateHasChanged);
                }

                break;
            case "End":
                if (_open)
                {
                    _activeIndex = FindEnabledIndex(_visibleItems.Count - 1, -1);
                    await InvokeAsync(StateHasChanged);
                }

                break;
            case "Enter":
            case " ":
                if (!_open)
                {
                    Open();
                    await InvokeAsync(StateHasChanged);
                }
                else if (_activeIndex >= 0 && _activeIndex < _visibleItems.Count)
                {
                    await ToggleItemAsync(_visibleItems[_activeIndex]);
                }

                break;
            case "Escape":
                if (_open)
                {
                    Close();
                    await InvokeAsync(StateHasChanged);
                }

                break;
            case "Backspace":
                if (!_open && HasSelection)
                {
                    await RemoveSelectedAsync(SelectedValues[^1]);
                }

                break;
        }
    }

    private Task HandlePopoverOpenChangedAsync(bool open)
    {
        if (open && !IsDisabled && !ReadOnly) Open();
        else Close();
        return InvokeAsync(StateHasChanged);
    }

    private void Open()
    {
        if (IsDisabled || ReadOnly || !Visible) return;
        _open = true;
        _activeIndex = _visibleItems.Count == 0
            ? -1
            : _activeIndex >= 0 && _activeIndex < _visibleItems.Count && !IsItemDisabled(_visibleItems[_activeIndex])
                ? _activeIndex
                : FindEnabledIndex(0, 1);
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

        var enabled = _visibleItems.Select((item, index) => (item, index)).Where(pair => !IsItemDisabled(pair.item)).Select(pair => pair.index).ToArray();
        if (enabled.Length == 0) { _activeIndex = -1; return; }
        var current = Array.IndexOf(enabled, _activeIndex);
        var origin = current < 0 ? (direction > 0 ? -1 : 0) : current;
        _activeIndex = enabled[(origin + direction + enabled.Length) % enabled.Length];
    }

    private int FindEnabledIndex(int start, int direction)
    {
        for (var index = start; index >= 0 && index < _visibleItems.Count; index += direction)
        {
            if (!IsItemDisabled(_visibleItems[index])) return index;
        }

        return -1;
    }

    private async Task ToggleItemAsync(TItem item)
    {
        if (IsDisabled || ReadOnly || IsItemDisabled(item)) return;

        var next = SelectedValues.ToList();
        var selectedIndex = next.FindIndex(value => ValuesEqual(value, item));
        if (selectedIndex >= 0) next.RemoveAt(selectedIndex);
        else next.Add(item);
        await SetSelectedValuesAsync(next, item);
    }

    private async Task SelectAllAsync(MouseEventArgs _)
    {
        if (IsDisabled || ReadOnly) return;
        var next = _visibleItems.Where(item => !IsItemDisabled(item)).Distinct().ToArray();
        await SetSelectedValuesAsync(next);
    }

    private async Task ClearAllAsync(MouseEventArgs _)
    {
        if (IsDisabled || ReadOnly || !HasSelection) return;
        await SetSelectedValuesAsync([], null, clear: true);
    }

    private async Task RemoveSelectedAsync(TItem item)
    {
        if (IsDisabled || ReadOnly) return;
        var next = SelectedValues.Where(value => !ValuesEqual(value, item)).ToArray();
        if (next.Length == SelectedValues.Count) return;
        await SetSelectedValuesAsync(next, item);
    }

    private async Task SetSelectedValuesAsync(IReadOnlyList<TItem> next, TItem? item = null, bool clear = false)
    {
        SelectedValues = next;
        await SelectedValuesChanged.InvokeAsync(next);
        NotifyFieldChanged();
        if (item is not null && OnItemSelected.HasDelegate)
        {
            await OnItemSelected.InvokeAsync(item);
        }

        if (clear && OnClear.HasDelegate)
        {
            await OnClear.InvokeAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private void RebuildItems()
    {
        _visibleItems.Clear();
        _visibleItems.AddRange(Items);
    }

    private bool IsItemDisabled(TItem item) => DisabledSelector?.Invoke(item) == true;

    private bool IsSelected(TItem item) => SelectedValues.Any(value => ValuesEqual(value, item));

    private string DisplayText(TItem item) => TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty;

    private string? SizeClass => ComponentClass.ForSize("aeterni-multi-select", Size);

    private void UpdateEditContextSubscription()
    {
        var nextEditContext = SelectedValuesExpression is null ? null : CascadedEditContext;
        if (!ReferenceEquals(_subscribedEditContext, nextEditContext))
        {
            UnsubscribeFromEditContext();
            _subscribedEditContext = nextEditContext;
            if (_subscribedEditContext is not null)
            {
                _subscribedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
            }
        }

        if (SelectedValuesExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(SelectedValuesExpression);
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

    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier && _subscribedEditContext is not null)
        {
            _subscribedEditContext.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        if (!IsDisposed) _ = InvokeAsync(StateHasChanged);
    }

    private static bool ValuesEqual(TItem? left, TItem? right) => EqualityComparer<TItem>.Default.Equals(left!, right!);
}
