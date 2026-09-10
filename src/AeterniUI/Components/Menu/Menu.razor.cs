using AeterniUI.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Menu;

public sealed record MenuItem(string Id, string Label, string? Description = null, RenderFragment? Icon = null, bool Disabled = false);
public sealed record MenuGroup(string Key, string Label, IReadOnlyList<MenuItem> Items, RenderFragment? Icon = null, bool InitiallyOpen = true);

[JsModule("Components/Menu/Menu.razor.js", Name = "menu")]
public partial class Menu : AeterniComponent
{
    private const string ModuleName = "menu";

    /// <summary>Item slot value used when the focused node is a group toggle.</summary>
    private const int NoItem = -1;

    /// <summary>Sentinel for "no menu node has been focused yet".</summary>
    private const int NothingFocused = -1;

    [Parameter, EditorRequired] public IReadOnlyList<MenuGroup> Items { get; set; } = [];
    [Parameter] public string? SelectedId { get; set; }
    [Parameter] public EventCallback<string> SelectedIdChanged { get; set; }
    [Parameter] public EventCallback<MenuItem> ItemSelected { get; set; }
    [Parameter] public bool Accordion { get; set; }
    [Parameter] public string? AriaLabel { get; set; }

    private readonly HashSet<string> _open = [];
    private bool _initialized;
    private int _focusedGroupIndex = NothingFocused;
    private int _focusedItemIndex = NothingFocused;

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-menu").Add("is-accordion", Accordion);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!_initialized)
        {
            _initialized = true;

            // Accordion keeps a single group open, so only the first requested
            // group is expanded on load; the selected group opens below.
            foreach (var group in Items.Where(group => group.InitiallyOpen).Take(Accordion ? 1 : int.MaxValue))
            {
                _open.Add(group.Key);
            }
        }

        PruneOpenGroups();

        if (!string.IsNullOrWhiteSpace(SelectedId))
        {
            foreach (var group in Items.Where(group => group.Items.Any(item => item.Id == SelectedId)))
            {
                _open.Add(group.Key);
                if (Accordion) _open.RemoveWhere(key => key != group.Key);
            }
        }
    }

    private bool IsOpen(string key) => _open.Contains(key);

    /// <summary>Drops expansion state for groups that are no longer rendered.</summary>
    private void PruneOpenGroups()
    {
        if (_open.Count == 0)
        {
            return;
        }

        var keys = Items.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        _open.RemoveWhere(key => !keys.Contains(key));
    }

    private string GroupToggleId(int groupIndex) => $"{ElementId}-group-{groupIndex}";

    private string GroupRegionId(int groupIndex) => $"{ElementId}-group-{groupIndex}-items";

    private string ItemId(int groupIndex, int itemIndex) => $"{ElementId}-group-{groupIndex}-item-{itemIndex}";

    private void TrackFocus(int groupIndex, int itemIndex)
    {
        _focusedGroupIndex = groupIndex;
        _focusedItemIndex = itemIndex;
    }

    private async Task ToggleGroupAsync(string key)
    {
        if (_open.Contains(key))
        {
            _open.Remove(key);
        }
        else
        {
            if (Accordion) _open.Clear();
            _open.Add(key);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectAsync(MenuItem item)
    {
        if (item.Disabled)
        {
            return;
        }

        await SelectedIdChanged.InvokeAsync(item.Id);
        await ItemSelected.InvokeAsync(item);
    }

    /// <summary>
    /// Keyboard model for a disclosure navigation menu: the buttons stay in the
    /// tab order, and the arrow keys additionally move focus between the nodes
    /// that are currently reachable (group toggles plus the items of open
    /// groups). Enter and Space keep their native button behaviour.
    /// </summary>
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        var nodes = BuildVisibleNodes();
        if (nodes.Count == 0)
        {
            return;
        }

        var current = nodes.IndexOf((_focusedGroupIndex, _focusedItemIndex));
        if (current < 0 && _focusedGroupIndex >= 0)
        {
            // Focus sat inside a group that was just collapsed: fall back to that
            // group's toggle instead of jumping to the first node.
            current = nodes.FindIndex(node => node.GroupIndex == _focusedGroupIndex);
        }

        switch (args.Key)
        {
            case "ArrowDown":
                await FocusNodeAsync(nodes, current < 0 ? 0 : (current + 1) % nodes.Count);
                break;

            case "ArrowUp":
                await FocusNodeAsync(nodes, current < 0 ? nodes.Count - 1 : (current - 1 + nodes.Count) % nodes.Count);
                break;

            case "Home":
                await FocusNodeAsync(nodes, 0);
                break;

            case "End":
                await FocusNodeAsync(nodes, nodes.Count - 1);
                break;

            case "ArrowRight":
                await ExpandFocusedGroupAsync();
                break;

            case "ArrowLeft":
                await CollapseFocusedGroupAsync(nodes);
                break;
        }
    }

    /// <summary>
    /// Lists the nodes reachable with the arrow keys in render order. Disabled
    /// items are skipped because they are not focusable either.
    /// </summary>
    private List<(int GroupIndex, int ItemIndex)> BuildVisibleNodes()
    {
        var nodes = new List<(int GroupIndex, int ItemIndex)>();

        for (var groupIndex = 0; groupIndex < Items.Count; groupIndex++)
        {
            var group = Items[groupIndex];
            nodes.Add((groupIndex, NoItem));

            if (!IsOpen(group.Key))
            {
                continue;
            }

            for (var itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
            {
                if (!group.Items[itemIndex].Disabled)
                {
                    nodes.Add((groupIndex, itemIndex));
                }
            }
        }

        return nodes;
    }

    private async Task FocusNodeAsync(List<(int GroupIndex, int ItemIndex)> nodes, int index)
    {
        if (index < 0 || index >= nodes.Count)
        {
            return;
        }

        var (groupIndex, itemIndex) = nodes[index];
        TrackFocus(groupIndex, itemIndex);

        var nodeId = itemIndex == NoItem ? GroupToggleId(groupIndex) : ItemId(groupIndex, itemIndex);
        await JsModuleManager.InvokeModuleVoidAsync(ModuleName, "focus", nodeId);
    }

    private async Task ExpandFocusedGroupAsync()
    {
        if (_focusedItemIndex != NoItem || _focusedGroupIndex < 0 || _focusedGroupIndex >= Items.Count)
        {
            return;
        }

        var group = Items[_focusedGroupIndex];
        if (IsOpen(group.Key))
        {
            return;
        }

        await ToggleGroupAsync(group.Key);
    }

    private async Task CollapseFocusedGroupAsync(List<(int GroupIndex, int ItemIndex)> nodes)
    {
        if (_focusedGroupIndex < 0 || _focusedGroupIndex >= Items.Count)
        {
            return;
        }

        var group = Items[_focusedGroupIndex];

        if (_focusedItemIndex != NoItem)
        {
            // Inside an expanded group, ArrowLeft returns focus to its toggle.
            await FocusNodeAsync(nodes, nodes.FindIndex(node => node == (_focusedGroupIndex, NoItem)));
            return;
        }

        if (IsOpen(group.Key))
        {
            await ToggleGroupAsync(group.Key);
        }
    }
}
