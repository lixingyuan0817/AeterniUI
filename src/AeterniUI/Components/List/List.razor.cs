using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.List;

public partial class List : AeterniComponent
{
    private readonly List<ItemHandle> _items = [];
    private ItemHandle? _focusedItem;

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

    internal async Task ToggleAsync(ItemHandle item)
    {
        if (item.Disabled || !IsSelectable)
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

                var snapshot = next as IReadOnlyList<object?>;
                await SelectedValuesChanged.InvokeAsync(snapshot!);
                break;
        }

        if (OnItemSelected.HasDelegate)
        {
            await OnItemSelected.InvokeAsync(item.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    internal void Register(ItemHandle item)
    {
        if (!_items.Contains(item))
        {
            _items.Add(item);
        }
    }

    internal void Unregister(ItemHandle item) => _items.Remove(item);

    internal void SetActive(ItemHandle? item)
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

    internal bool IsActive(ItemHandle item) => ReferenceEquals(_focusedItem, item);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(SelectionMode))
        {
            throw new ArgumentOutOfRangeException(nameof(SelectionMode), SelectionMode, "Unknown list selection mode.");
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-list")
            .Add("aeterni-list--select", IsSelectable);
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
            attributes["tabindex"] = 0;

            // Tell assistive technology which option the arrow keys are on.
            if (ActiveOptionId is not null)
            {
                attributes["aria-activedescendant"] = ActiveOptionId;
            }
        }

        return attributes;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!IsSelectable)
        {
            return;
        }

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

        if (SelectionMode == SelectionMode.Single && next.Value is not null)
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
        _items.Clear();
        _focusedItem = null;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Internal registration record kept by a <see cref="List"/> for focus
/// management and keyboard navigation. Order follows DOM render order.
/// </summary>
internal sealed class ItemHandle
{
    public ItemHandle(ListItem item, object? value, bool disabled)
    {
        Item = item;
        Value = value;
        Disabled = disabled;
    }

    public ListItem Item { get; }

    public object? Value { get; }

    public string? OptionId { get; set; }

    public bool Disabled { get; set; }
}
