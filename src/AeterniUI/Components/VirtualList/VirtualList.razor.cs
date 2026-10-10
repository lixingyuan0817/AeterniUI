using AeterniUI.Attributes;
using AeterniUI.Enums;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.VirtualList;

/// <summary>
/// A fixed-row-height virtualized list for large in-memory collections.
/// </summary>
[JsModule("Components/VirtualList/VirtualList.razor.js", Name = "virtual-list", Interactive = true)]
public partial class VirtualList<TItem> : AeterniComponent
{
    private readonly record struct VirtualRow(int Index, TItem Value, object Key);

    private List<VirtualRow> _rows = [];
    private float _itemSizePixels;
    private int _activeIndex = -1;
    private int? _scrollRequestIndex;
    private bool _loadMoreInFlight;
    private bool _disposed;

    [Parameter, EditorRequired]
    public IReadOnlyList<TItem>? Items { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public string Height { get; set; } = "320px";

    [Parameter]
    public double ItemSize { get; set; } = 32;

    [Parameter]
    public int OverscanCount { get; set; } = 5;

    [Parameter]
    public Func<TItem, object?>? ItemKeySelector { get; set; }

    [Parameter]
    public Func<TItem, bool>? DisabledSelector { get; set; }

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

    /// <summary>
    /// Indicates whether more items can be loaded when the scroll viewport reaches its end.
    /// </summary>
    [Parameter]
    public bool HasMoreItems { get; set; }

    /// <summary>
    /// Indicates that the host is currently loading more items.
    /// </summary>
    [Parameter]
    public bool LoadingMore { get; set; }

    /// <summary>
    /// The distance, in CSS pixels, from the end of the viewport at which <see cref="OnLoadMore"/> is raised.
    /// </summary>
    [Parameter]
    public double LoadMoreThreshold { get; set; } = 96;

    /// <summary>
    /// Raised once when the viewport reaches the configured load-more threshold.
    /// The host owns data loading and should append items through <see cref="Items"/>.
    /// </summary>
    [Parameter]
    public EventCallback OnLoadMore { get; set; }

    internal bool IsSelectable => SelectionMode != SelectionMode.None;

    private string ItemRole => IsSelectable ? "option" : "listitem";

    private string? ActiveOptionId => IsSelectable && _activeIndex >= 0 && _activeIndex < _rows.Count
        ? OptionId(_rows[_activeIndex])
        : null;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (string.IsNullOrWhiteSpace(Height))
        {
            throw new ArgumentException("VirtualList requires a non-empty Height.", nameof(Height));
        }

        if (!double.IsFinite(ItemSize) || ItemSize <= 0 || ItemSize > float.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(ItemSize), ItemSize, "ItemSize must be greater than zero.");
        }

        if (OverscanCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(OverscanCount), OverscanCount, "OverscanCount cannot be negative.");
        }

        if (!double.IsFinite(LoadMoreThreshold) || LoadMoreThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LoadMoreThreshold), LoadMoreThreshold, "LoadMoreThreshold must be zero or greater.");
        }

        if (!Enum.IsDefined(SelectionMode))
        {
            throw new ArgumentOutOfRangeException(nameof(SelectionMode), SelectionMode, "Unknown virtual list selection mode.");
        }

        var previousActiveKey = _activeIndex >= 0 && _activeIndex < _rows.Count ? _rows[_activeIndex].Key : null;
        _itemSizePixels = (float)ItemSize;
        _rows = BuildRows(Items ?? []);
        if (Disabled || !Visible || !IsSelectable)
        {
            _activeIndex = -1;
        }
        else
        {
            _activeIndex = previousActiveKey is null
                ? (_rows.Count == 0 ? -1 : Math.Clamp(_activeIndex, -1, _rows.Count - 1))
                : _rows.FindIndex(row => Equals(row.Key, previousActiveKey));
        }

        if (Items is not null)
        {
            await PruneSelectionAsync();
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-virtual-list")
        .Add("is-selectable", IsSelectable)
        .Add("is-disabled", Disabled)
        .Add("is-loading-more", LoadingMore)
        .Add("is-empty", _rows.Count == 0);

    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-virtual-list-height", Height.Trim(), true)
        .Add("--aeterni-virtual-list-item-size", $"{_itemSizePixels.ToString(CultureInfo.InvariantCulture)}px");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = IsSelectable ? "listbox" : "list",
            ["tabindex"] = IsSelectable && !Disabled ? 0 : -1
        };

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel.Trim();
        }

        if (IsSelectable)
        {
            attributes["aria-multiselectable"] = SelectionMode == SelectionMode.Multiple ? "true" : "false";
            if (ActiveOptionId is not null)
            {
                attributes["aria-activedescendant"] = ActiveOptionId;
            }
        }

        if (Disabled)
        {
            attributes["aria-disabled"] = "true";
        }

        if (LoadingMore)
        {
            attributes["aria-busy"] = "true";
        }

        return attributes;
    }

    private List<VirtualRow> BuildRows(IReadOnlyList<TItem> items)
    {
        var rows = new List<VirtualRow>(items.Count);
        var keys = ItemKeySelector is null ? null : new HashSet<object?>();

        for (var index = 0; index < items.Count; index++)
        {
            var value = items[index];
            var key = ItemKeySelector?.Invoke(value);
            if (ItemKeySelector is not null && key is null)
            {
                throw new ArgumentException("ItemKeySelector must return a non-null key for every item.", nameof(ItemKeySelector));
            }

            key ??= index;
            if (keys is not null && !keys.Add(key))
            {
                throw new ArgumentException("ItemKeySelector must return unique keys.", nameof(ItemKeySelector));
            }

            rows.Add(new VirtualRow(index, value, key));
        }

        return rows;
    }

    private string OptionId(VirtualRow row) => $"{ElementId}-option-{row.Index}";

    private bool IsRowDisabled(VirtualRow row) => Disabled || (DisabledSelector?.Invoke(row.Value) ?? false);

    private bool IsRowSelected(VirtualRow row) => SelectionMode switch
    {
        SelectionMode.Single => ValuesEqual(SelectedValue, row.Value),
        SelectionMode.Multiple => SelectedValues?.Any(value => ValuesEqual(value, row.Value)) == true,
        _ => false
    };

    private bool IsRowActive(VirtualRow row) => row.Index == _activeIndex;

    private string RowClass(VirtualRow row) => ClassBuilder("aeterni-virtual-list__item")
        .Add("is-selected", IsRowSelected(row))
        .Add("is-active", IsRowActive(row))
        .Add("is-disabled", IsRowDisabled(row))
        .Build();

    private string? SelectedAttribute(VirtualRow row) => IsSelectable ? (IsRowSelected(row) ? "true" : "false") : null;

    private string? DisabledAttribute(VirtualRow row) => IsRowDisabled(row) ? "true" : null;

    private EventCallback<MouseEventArgs> ClickHandler(VirtualRow row) => IsSelectable
        ? EventCallback.Factory.Create<MouseEventArgs>(this, () => HandleClickAsync(row))
        : default;

    private async Task HandleClickAsync(VirtualRow row)
    {
        if (Disabled || IsRowDisabled(row)) return;
        // A clicked row is already visible. Requesting an indexed scroll here
        // aligned it to the viewport start, which made selection look like a
        // reorder and differed from List's stable pointer interaction.
        SetActive(row.Index, false);
        await ToggleAsync(row);
    }

    [JSInvokable]
    public async Task HandleKeyFromBrowserAsync(string key)
    {
        if (_disposed || !IsSelectable || Disabled || _rows.Count == 0) return;

        var enabled = _rows.Where(row => !IsRowDisabled(row)).ToArray();
        if (enabled.Length == 0) return;

        if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
        {
            var next = key switch
            {
                "Home" => enabled[0].Index,
                "End" => enabled[^1].Index,
                "ArrowDown" => NextIndex(enabled, 1),
                _ => NextIndex(enabled, -1)
            };
            SetActive(next, true);
            if (SelectionMode == SelectionMode.Single)
            {
                var nextRow = _rows.First(row => row.Index == next);
                await ToggleAsync(nextRow, allowClear: false);
            }
            else
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        else if (key is "Enter" or " ")
        {
            if (_activeIndex < 0 && SelectionMode == SelectionMode.Single)
            {
                var first = enabled[0];
                SetActive(first.Index, true);
                await ToggleAsync(first);
                return;
            }

            var row = _rows.FirstOrDefault(candidate => candidate.Index == _activeIndex);
            if (row.Key is not null && !IsRowDisabled(row))
            {
                await ToggleAsync(row);
            }
        }
    }

    private int NextIndex(IReadOnlyList<VirtualRow> enabled, int direction)
    {
        var current = Array.FindIndex(enabled.ToArray(), row => row.Index == _activeIndex);
        if (current < 0) return direction > 0 ? enabled[0].Index : enabled[^1].Index;
        return enabled[(current + direction + enabled.Count) % enabled.Count].Index;
    }

    private void SetActive(int index, bool requestScroll)
    {
        if (_activeIndex == index) return;
        _activeIndex = index;
        if (requestScroll) _scrollRequestIndex = index;
        RequestStateHasChanged();
    }

    private async Task ToggleAsync(VirtualRow row, bool allowClear = true)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                SelectedValue = allowClear && AllowClear && ValuesEqual(SelectedValue, row.Value) ? null : row.Value;
                await SelectedValueChanged.InvokeAsync(SelectedValue);
                break;
            case SelectionMode.Multiple:
                var next = (SelectedValues ?? []).ToList();
                var existing = next.FindIndex(value => ValuesEqual(value, row.Value));
                if (existing >= 0) next.RemoveAt(existing);
                else next.Add(row.Value);
                SelectedValues = next;
                await SelectedValuesChanged.InvokeAsync(SelectedValues);
                break;
        }

        if (OnItemSelected.HasDelegate)
        {
            await OnItemSelected.InvokeAsync(row.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task PruneSelectionAsync()
    {
        var values = _rows.Select(row => row.Value).ToArray();
        if (SelectedValue is not null && !values.Any(value => ValuesEqual(value, SelectedValue)))
        {
            SelectedValue = null;
            await SelectedValueChanged.InvokeAsync(null);
        }

        if (SelectedValues is not null)
        {
            var retained = SelectedValues.Where(selected => values.Any(value => ValuesEqual(value, selected))).ToArray();
            if (retained.Length != SelectedValues.Count)
            {
                SelectedValues = retained;
                await SelectedValuesChanged.InvokeAsync(retained);
            }
        }
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        var loadMoreEnabled = OnLoadMore.HasDelegate && HasMoreItems && !LoadingMore && !Disabled && Visible;
        await JsModuleManager.InvokeModuleVoidAsync(
            "virtual-list",
            "sync",
            InstanceId,
            RootElement,
            loadMoreEnabled,
            LoadMoreThreshold);
        if (_scrollRequestIndex is int index)
        {
            _scrollRequestIndex = null;
            await JsModuleManager.InvokeModuleVoidAsync("virtual-list", "scrollToIndex", InstanceId, index, _itemSizePixels);
        }
    }

    [JSInvokable]
    public async Task HandleLoadMoreAsync()
    {
        if (_disposed || _loadMoreInFlight || !OnLoadMore.HasDelegate || !HasMoreItems || LoadingMore || Disabled || !Visible)
        {
            return;
        }

        _loadMoreInFlight = true;
        try
        {
            await OnLoadMore.InvokeAsync();
        }
        finally
        {
            _loadMoreInFlight = false;
        }
    }

    private static bool ValuesEqual(object? left, object? right) => EqualityComparer<object?>.Default.Equals(left, right);

    // Mirrors List: the flag lets the JSInvokable entry points reject a callback
    // that the browser had already queued when the component was removed.
    protected override ValueTask OnComponentDisposeAsync()
    {
        _disposed = true;
        _rows.Clear();
        _activeIndex = -1;
        return ValueTask.CompletedTask;
    }
}
