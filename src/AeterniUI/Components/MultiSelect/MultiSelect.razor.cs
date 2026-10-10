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

    private readonly EditContextSubscription _validation;

    public MultiSelect() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

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

    private bool _open;
    private bool _keyboardNavigation;
    private int _activeIndex = -1;
    private ElementReference _triggerElement;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private string ListboxId => $"{ElementId}-list";

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsInvalid => Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private bool IsRequired => Required || (FormField?.Required ?? false);

    private bool HasSelection => SelectedValues.Count > 0;

    private bool HasEnabledItems => Items.Any(item => !IsItemDisabled(item));

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

    private string? ActiveOptionId => _open && _activeIndex >= 0 && _activeIndex < Items.Count
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
        UpdateEditContextSubscription();

        if (IsDisabled || !Visible || ReadOnly)
        {
            _open = false;
            _activeIndex = -1;
        }
        else if (_open && _keyboardNavigation)
        {
            _activeIndex = Items.Count == 0
                ? -1
                : _activeIndex >= 0 && _activeIndex < Items.Count && !IsItemDisabled(Items[_activeIndex])
                    ? _activeIndex
                    : FindEnabledIndex(0, 1);
        }
        else if (_open)
        {
            _activeIndex = -1;
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _open = false;
        UnsubscribeFromEditContext();
        return ValueTask.CompletedTask;
    }

    // The trigger is passed explicitly instead of being looked up from
    // RootElement: a consumer-supplied Element parameter replaces RootElement,
    // and a querySelector would then silently find nothing and drop keyboard
    // suppression and active-option scrolling.
    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("multi-select", "sync", InstanceId, _triggerElement);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-multi-select")
        .Add(SizeClass)
        .Add("is-full-width", FullWidth)
        .Add("is-readonly", ReadOnly)
        .Add("is-open", _open)
        .Add("is-keyboard-navigation", _keyboardNavigation)
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

        _keyboardNavigation = false;
        if (_open)
        {
            Close();
        }
        else
        {
            // A pointer opening should not paint the first option as if it were
            // keyboard-active. Keyboard opening paths opt into an active item.
            Open(activateFirst: false);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsDisabled || ReadOnly || args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return;
        }

        _keyboardNavigation = true;
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
                    _activeIndex = FindEnabledIndex(Items.Count - 1, -1);
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
                else if (_activeIndex >= 0 && _activeIndex < Items.Count)
                {
                    await ToggleItemAsync(Items[_activeIndex]);
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
        if (open && !IsDisabled && !ReadOnly)
        {
            _keyboardNavigation = false;
            Open(activateFirst: false);
        }
        else Close();
        return InvokeAsync(StateHasChanged);
    }

    private void Open(bool activateFirst = true)
    {
        if (IsDisabled || ReadOnly || !Visible) return;
        _open = true;
        _activeIndex = !activateFirst || Items.Count == 0
            ? -1
            : _activeIndex >= 0 && _activeIndex < Items.Count && !IsItemDisabled(Items[_activeIndex])
                ? _activeIndex
                : FindEnabledIndex(0, 1);
    }

    private void Close()
    {
        _open = false;
        _keyboardNavigation = false;
        _activeIndex = -1;
    }

    private void MoveActive(int direction)
    {
        if (Items.Count == 0)
        {
            _activeIndex = -1;
            return;
        }

        var enabled = Items.Select((item, index) => (item, index)).Where(pair => !IsItemDisabled(pair.item)).Select(pair => pair.index).ToArray();
        if (enabled.Length == 0) { _activeIndex = -1; return; }
        var current = Array.IndexOf(enabled, _activeIndex);
        var origin = current < 0 ? (direction > 0 ? -1 : 0) : current;
        _activeIndex = enabled[(origin + direction + enabled.Length) % enabled.Length];
    }

    private int FindEnabledIndex(int start, int direction)
    {
        for (var index = start; index >= 0 && index < Items.Count; index += direction)
        {
            if (!IsItemDisabled(Items[index])) return index;
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

    private async Task HandleOptionClickAsync(TItem item)
    {
        _keyboardNavigation = false;
        await ToggleItemAsync(item);
    }

    private async Task SelectAllAsync(MouseEventArgs _)
    {
        if (IsDisabled || ReadOnly) return;
        var next = Items.Where(item => !IsItemDisabled(item)).Distinct().ToArray();
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

    private bool IsItemDisabled(TItem item) => DisabledSelector?.Invoke(item) == true;

    private bool IsSelected(TItem item) => SelectedValues.Any(value => ValuesEqual(value, item));

    private string DisplayText(TItem item) => TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty;

    private string? SizeClass => ComponentClass.ForSize("aeterni-multi-select", Size);

    private string PopupHostStyle => FullWidth
        ? "width: 100%; max-width: 100%;"
        : "width: min(100%, var(--aeterni-width-input-md)); max-width: 100%;";

    private void UpdateEditContextSubscription()
    {
        _validation.Attach(SelectedValuesExpression is null ? null : CascadedEditContext);

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


    private void UnsubscribeFromEditContext() => _validation.Detach();


    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier)
        {
            _validation.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    private static bool ValuesEqual(TItem? left, TItem? right) => EqualityComparer<TItem>.Default.Equals(left!, right!);
}
