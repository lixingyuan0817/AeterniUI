using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.List;

[CascadingTypeParameter(nameof(TItem))]
[JsModule("Components/List/List.razor.js", Name = "list", Interactive = true)]
public partial class List<TItem> : AeterniComponent
{
    private readonly System.Collections.Generic.List<ItemHandle<TItem>> _items = [];
    private ItemHandle<TItem>? _focusedItem;

    private TItem[] _dataItems = [];
    private bool _disposed;

    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter]
    public bool CardMode { get; set; }

    /// <summary>
    /// Maximum block size of the list viewport. When specified, the list owns
    /// vertical scrolling once its content exceeds this size.
    /// </summary>
    [Parameter]
    public string? MaxHeight { get; set; }

    [Parameter]
    public RenderFragment<TItem>? CardTemplate { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public SelectionMode SelectionMode { get; set; } = SelectionMode.None;

    /// <summary>
    /// 类型化单选值：用 <c>@bind-SelectedItem</c> 绑定，回调参数是 <c>TItem?</c> 而不是
    /// <c>object?</c>，Items 模式下宿主因此保留编译期类型安全。
    /// </summary>
    /// <remarks>
    /// 同时提供旧的 <see cref="SelectedValue" /> 时以类型化成员为准。新代码应使用
    /// <c>SelectedItem</c>/<c>SelectedItems</c>；旧成员只保留给声明式
    /// <c>ListItem.Value</c> 中无法转成 <c>TItem</c> 的 object 值。绑定类型化回调后，
    /// 每个选中值都必须能表示为 <c>TItem</c>，否则组件直接抛错，而不是悄悄返回默认值。
    /// </remarks>
    [Parameter]
    public TItem? SelectedItem { get; set; }

    /// <summary>类型化单选值发生变化时触发；配合 <c>@bind-SelectedItem</c> 使用。</summary>
    [Parameter]
    public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>
    /// 类型化多选值：用 <c>@bind-SelectedItems</c> 绑定，优先级与取值约束同
    /// <see cref="SelectedItem" />。
    /// </summary>
    [Parameter]
    public IReadOnlyList<TItem> SelectedItems { get; set; } = [];

    /// <summary>类型化多选值发生变化时触发；配合 <c>@bind-SelectedItems</c> 使用。</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    // The object-based members stay the compatibility surface: declarative ListItem
    // values are still object?, so they cannot be typed at the List level without
    // breaking the declarative mode. Both facades write the same object-typed
    // selection state below, so they cannot drift apart.
    private object? _legacySelectedValue;
    private EventCallback<object?> _legacySelectedValueChanged;
    private IReadOnlyList<object?>? _legacySelectedValues;
    private EventCallback<IReadOnlyList<object?>> _legacySelectedValuesChanged;

    /// <summary>
    /// Compatibility single-selection value (object-typed). Prefer
    /// <see cref="SelectedItem" /> / <see cref="SelectedItemChanged" />; this pair
    /// remains for declarative <c>ListItem.Value</c> values and for hosts migrating
    /// from the object API.
    /// </summary>
    [Parameter, Obsolete("Use SelectedItem / SelectedItemChanged for type-safe selection. SelectedValue stays for object-typed declarative ListItem values.")]
    public object? SelectedValue
    {
        get => _legacySelectedValue;
        set => _legacySelectedValue = value;
    }

    /// <summary>Compatibility callback for <see cref="SelectedValue" />.</summary>
    [Parameter, Obsolete("Use SelectedItemChanged; SelectedValueChanged stays for object-typed declarative ListItem values.")]
    public EventCallback<object?> SelectedValueChanged
    {
        get => _legacySelectedValueChanged;
        set => _legacySelectedValueChanged = value;
    }

    /// <summary>
    /// Compatibility multiple-selection value (object-typed). Prefer
    /// <see cref="SelectedItems" /> / <see cref="SelectedItemsChanged" />.
    /// </summary>
    [Parameter, Obsolete("Use SelectedItems / SelectedItemsChanged for type-safe selection. SelectedValues stays for object-typed declarative ListItem values.")]
    public IReadOnlyList<object?>? SelectedValues
    {
        get => _legacySelectedValues;
        set => _legacySelectedValues = value;
    }

    /// <summary>Compatibility callback for <see cref="SelectedValues" />.</summary>
    [Parameter, Obsolete("Use SelectedItemsChanged; SelectedValuesChanged stays for object-typed declarative ListItem values.")]
    public EventCallback<IReadOnlyList<object?>> SelectedValuesChanged
    {
        get => _legacySelectedValuesChanged;
        set => _legacySelectedValuesChanged = value;
    }

    [Parameter]
    public bool AllowClear { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public EventCallback<object?> OnItemSelected { get; set; }

    internal bool IsSelectable => SelectionMode != SelectionMode.None;

    private string? ActiveOptionId => _focusedItem?.OptionId;

    internal bool IsSelected(object? value) => SelectionMode switch
    {
        SelectionMode.Single => ValuesEqual(_legacySelectedValue, value),
        SelectionMode.Multiple => _legacySelectedValues?.Any(selected => ValuesEqual(selected, value)) == true,
        _ => false
    };

    internal async Task ToggleAsync(ItemHandle<TItem> item, bool allowClear = true)
    {
        if (Disabled || !Visible || !IsEligible(item) || !IsSelectable || !_items.Contains(item))
        {
            return;
        }

        switch (SelectionMode)
        {
            case SelectionMode.Single:
                var single = allowClear && AllowClear && ValuesEqual(_legacySelectedValue, item.Value)
                    ? null
                    : item.Value;
                await CommitSingleSelectionAsync(single);
                break;

            case SelectionMode.Multiple:
                var next = (_legacySelectedValues ?? []).ToList();
                if (next.Any(selected => ValuesEqual(selected, item.Value)))
                {
                    next.RemoveAll(selected => ValuesEqual(selected, item.Value));
                }
                else
                {
                    next.Add(item.Value);
                }

                await CommitMultipleSelectionAsync(next);
                break;
        }

        if (OnItemSelected.HasDelegate)
        {
            await OnItemSelected.InvokeAsync(item.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    internal void Register(ItemHandle<TItem> item)
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }
    }

    internal void Unregister(ItemHandle<TItem> item)
    {
        _items.Remove(item);
        InvalidateActive(item);
    }

    internal void InvalidateActive(ItemHandle<TItem> item)
    {
        if (ReferenceEquals(_focusedItem, item))
        {
            _focusedItem = null;
            RequestStateHasChanged();
        }
    }

    internal void SetActive(ItemHandle<TItem>? item)
    {
        if (ReferenceEquals(_focusedItem, item))
        {
            return;
        }

        _focusedItem = item;

        // Focus stays on the listbox container, so the active option has to
        // re-render itself to show the keyboard cue.
        foreach (var handle in _items)
        {
            handle.Item.Refresh();
        }
    }

    internal bool IsActive(ItemHandle<TItem> item) => ReferenceEquals(_focusedItem, item);

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (!Enum.IsDefined(SelectionMode))
        {
            throw new ArgumentOutOfRangeException(nameof(SelectionMode), SelectionMode, "Unknown list selection mode.");
        }

        ApplyTypedSelectionParameters();

        var next = Items?.ToArray() ?? [];
        if (!_dataItems.SequenceEqual(next) || Disabled || !Visible || !IsSelectable)
        {
            _focusedItem = null;
        }
        _dataItems = next;
        if (Items is not null)
        {
            if (_legacySelectedValue is not null && !next.Any(item => ValuesEqual(item, _legacySelectedValue)))
            {
                await CommitSingleSelectionAsync(null);
            }
            if (_legacySelectedValues is not null)
            {
                var retained = _legacySelectedValues.Where(value => next.Any(item => ValuesEqual(item, value))).ToArray();
                if (retained.Length != _legacySelectedValues.Count)
                {
                    await CommitMultipleSelectionAsync(retained);
                }
            }
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-list")
            .Add("aeterni-list--select", IsSelectable)
            .Add("aeterni-list--cards", CardMode)
            .Add("aeterni-list--scrollable", !string.IsNullOrWhiteSpace(MaxHeight));
    }

    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-list-max-height", MaxHeight, !string.IsNullOrWhiteSpace(MaxHeight));

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (IsSelectable)
        {
            attributes["role"] = "listbox";

            if (!string.IsNullOrWhiteSpace(AriaLabel))
            {
                attributes["aria-label"] = AriaLabel;
            }

            attributes["aria-multiselectable"] = SelectionMode == SelectionMode.Multiple ? "true" : "false";
            attributes["tabindex"] = Disabled ? -1 : 0;
            if (Disabled) attributes["aria-disabled"] = "true";

            // Tell assistive technology which option the arrow keys are on.
            if (ActiveOptionId is not null)
            {
                attributes["aria-activedescendant"] = ActiveOptionId;
            }
        }

        return attributes;
    }

    /// <summary>
    /// Handles browser navigation in current DOM order. The module only installs
    /// a listener on selectable lists; business selection remains in C#.
    /// </summary>
    // DOM order is supplied by the browser, including keyed/moved declarative rows.
    // Only registered, currently available options may participate in selection.
    [JSInvokable]
    public Task HandleKeyFromBrowserAsync(string key, string[] optionIds)
    {
        var byId = _items.Where(IsEligible).Where(item => item.OptionId is not null)
            .ToDictionary(item => item.OptionId!);
        var enabled = optionIds.Distinct().Where(byId.ContainsKey).Select(id => byId[id]).ToArray();
        return HandleKeyAsync(key, enabled);
    }

    private static bool IsEligible(ItemHandle<TItem> item) => !item.Disabled && item.Item.Visible;

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("list", "sync", InstanceId, RootElement);

    private async Task HandleKeyAsync(string key, ItemHandle<TItem>[] enabled)
    {
        if (_disposed || Disabled || !Visible || !IsSelectable) return;

        if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
        {
            await MoveFocusAsync(key, enabled);
            return;
        }

        if (key is " " or "Enter")
        {
            if (_focusedItem is not null && enabled.Contains(_focusedItem))
            {
                await ToggleAsync(_focusedItem);
            }
            else if (SelectionMode == SelectionMode.Single)
            {
                // No active option yet: start from the first enabled item.
                var first = enabled.FirstOrDefault();
                if (first is not null)
                {
                    SetActive(first);
                    await ToggleAsync(first);
                }
            }

            return;
        }
    }

    private async Task MoveFocusAsync(string key, ItemHandle<TItem>[] enabled)
    {
        if (enabled.Length == 0)
        {
            return;
        }

        var currentIndex = _focusedItem is null
            ? -1
            : Array.FindIndex(enabled, item => item == _focusedItem);

        var nextIndex = key switch
        {
            "Home" => 0,
            "End" => enabled.Length - 1,
            "ArrowDown" => currentIndex + 1 < enabled.Length ? currentIndex + 1 : 0,
            "ArrowUp" => currentIndex - 1 >= 0 ? currentIndex - 1 : enabled.Length - 1,
            _ => currentIndex
        };

        var next = enabled[nextIndex];
        SetActive(next);

        if (SelectionMode == SelectionMode.Single)
        {
            await ToggleAsync(next, allowClear: false);
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    // ------------------------------------------------------------------ 选中桥接

    // The typed members only take over while they are actually in use. A host that
    // keeps using the legacy members (or a declarative list that never binds either)
    // must not have its object-typed selection reset by the typed defaults.
    private bool UsesTypedSingleSelection => SelectedItemChanged.HasDelegate || SelectedItem is not null;

    private bool UsesTypedMultipleSelection => SelectedItemsChanged.HasDelegate || SelectedItems.Count > 0;

    private void ApplyTypedSelectionParameters()
    {
        ArgumentNullException.ThrowIfNull(SelectedItems);

        if (UsesTypedSingleSelection && !ValuesEqual(SelectedItem, _legacySelectedValue))
        {
            _legacySelectedValue = SelectedItem;
        }

        if (!UsesTypedMultipleSelection)
        {
            return;
        }

        var current = _legacySelectedValues;
        var same = current is not null && current.Count == SelectedItems.Count;
        if (same)
        {
            for (var index = 0; index < SelectedItems.Count; index++)
            {
                if (!ValuesEqual(SelectedItems[index], current![index]))
                {
                    same = false;
                    break;
                }
            }
        }

        if (!same)
        {
            _legacySelectedValues = SelectedItems.Cast<object?>().ToArray();
        }
    }

    private async Task CommitSingleSelectionAsync(object? value)
    {
        // Convert before mutating so an object-typed declarative value that cannot be
        // represented as TItem fails without desynchronising the rendered selection.
        var notifyTyped = SelectedItemChanged.HasDelegate;
        var typed = notifyTyped ? ToTypedItem(value) : default(TItem?);

        _legacySelectedValue = value;
        await _legacySelectedValueChanged.InvokeAsync(value);

        if (notifyTyped)
        {
            SelectedItem = typed;
            await SelectedItemChanged.InvokeAsync(typed);
        }
    }

    private async Task CommitMultipleSelectionAsync(IReadOnlyList<object?> values)
    {
        IReadOnlyList<TItem>? typed = SelectedItemsChanged.HasDelegate ? ToTypedItems(values) : null;

        _legacySelectedValues = values;
        await _legacySelectedValuesChanged.InvokeAsync(values);

        if (typed is not null)
        {
            SelectedItems = typed;
            await SelectedItemsChanged.InvokeAsync(typed);
        }
    }

    private static TItem? ToTypedItem(object? value)
    {
        if (value is null)
        {
            return default;
        }

        var target = Nullable.GetUnderlyingType(typeof(TItem)) ?? typeof(TItem);
        if (!target.IsInstanceOfType(value))
        {
            throw new InvalidOperationException(
                $"The selected value of type '{value.GetType()}' cannot be represented as '{typeof(TItem)}'. " +
                "Typed selection requires values assignable to TItem; use SelectedValue/SelectedValues for " +
                "object-typed ListItem values, or make ListItem.Value assignable to TItem.");
        }

        return (TItem)value;
    }

    private static IReadOnlyList<TItem> ToTypedItems(IReadOnlyList<object?> values)
    {
        var typed = new TItem[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            typed[index] = ToTypedItem(values[index])!;
        }

        return typed;
    }

    private static bool ValuesEqual(object? left, object? right) =>
        EqualityComparer<object?>.Default.Equals(left, right);

    protected override ValueTask OnComponentDisposeAsync()
    {
        _disposed = true;
        _items.Clear();
        _focusedItem = null;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Internal registration record kept by a <see cref="List{TItem}"/> for focus
/// management and keyboard navigation. Browser navigation supplies current DOM order.
/// </summary>
internal sealed class ItemHandle<TItem>
{
    public ItemHandle(ListItem<TItem> item, object? value, bool disabled)
    {
        Item = item;
        Value = value;
        Disabled = disabled;
    }

    public ListItem<TItem> Item { get; }

    public object? Value { get; set; }

    public string? OptionId { get; set; }

    public bool Disabled { get; set; }
}
