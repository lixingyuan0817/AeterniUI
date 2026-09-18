using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.List;

[CascadingTypeParameter(nameof(TItem))]
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

    [Parameter]
    public RenderFragment<TItem>? CardTemplate { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public SelectionMode SelectionMode { get; set; } = SelectionMode.None;

    [Parameter]
    public object? SelectedValue { get; set; }

    [Parameter]
    public EventCallback<object?> SelectedValueChanged { get; set; }

    [Parameter]
    public IReadOnlyList<object?>? SelectedValues { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<object?>> SelectedValuesChanged { get; set; }

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
        SelectionMode.Single => ValuesEqual(SelectedValue, value),
        SelectionMode.Multiple => SelectedValues?.Any(selected => ValuesEqual(selected, value)) == true,
        _ => false
    };

    internal async Task ToggleAsync(ItemHandle<TItem> item)
    {
        if (Disabled || item.Disabled || !IsSelectable || !_items.Contains(item))
        {
            return;
        }

        switch (SelectionMode)
        {
            case SelectionMode.Single:
                if (AllowClear && ValuesEqual(SelectedValue, item.Value))
                {
                    SelectedValue = null;
                }
                else
                {
                    SelectedValue = item.Value;
                }

                await SelectedValueChanged.InvokeAsync(SelectedValue);
                break;

            case SelectionMode.Multiple:
                var next = (SelectedValues ?? []).ToList();
                if (next.Any(selected => ValuesEqual(selected, item.Value)))
                {
                    next.RemoveAll(selected => ValuesEqual(selected, item.Value));
                }
                else
                {
                    next.Add(item.Value);
                }

                SelectedValues = next;
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
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
            if (!_disposed) _ = InvokeAsync(StateHasChanged);
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

        var next = Items?.ToArray() ?? [];
        if (!_dataItems.SequenceEqual(next) || Disabled || !IsSelectable)
        {
            _focusedItem = null;
        }
        _dataItems = next;
        if (Items is not null)
        {
            if (SelectedValue is not null && !next.Any(item => ValuesEqual(item, SelectedValue)))
            {
                SelectedValue = null;
                await SelectedValueChanged.InvokeAsync(null);
            }
            if (SelectedValues is not null)
            {
                var retained = SelectedValues.Where(value => next.Any(item => ValuesEqual(item, value))).ToArray();
                if (retained.Length != SelectedValues.Count)
                {
                    SelectedValues = retained;
                    await SelectedValuesChanged.InvokeAsync(retained);
                }
            }
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-list")
            .Add("aeterni-list--select", IsSelectable)
            .Add("aeterni-list--cards", CardMode);
    }

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
    /// Keyboard handler that is only bound for a selectable list. A default
    /// <see cref="EventCallback{T}" /> renders no attribute at all, so a display-only
    /// list registers no DOM listener.
    /// </summary>
    private EventCallback<KeyboardEventArgs> KeyDownHandler => IsSelectable
        ? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync)
        : default;

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled) return;

        if (args.Key is "ArrowDown" or "ArrowUp" or "Home" or "End")
        {
            await MoveFocusAsync(args.Key);
            return;
        }

        if (args.Key is " " or "Enter")
        {
            if (_focusedItem is not null)
            {
                await ToggleAsync(_focusedItem);
            }
            else if (SelectionMode == SelectionMode.Single)
            {
                // No active option yet: start from the first enabled item.
                var first = _items.FirstOrDefault(item => !item.Disabled);
                if (first is not null)
                {
                    await ToggleAsync(first);
                }
            }

            return;
        }
    }

    private async Task MoveFocusAsync(string key)
    {
        var enabled = _items.Where(item => !item.Disabled).ToArray();
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
            await ToggleAsync(next);
            return;
        }

        await InvokeAsync(StateHasChanged);
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
/// management and keyboard navigation. Order follows DOM render order.
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
