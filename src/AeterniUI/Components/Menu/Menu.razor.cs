using AeterniUI.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Menu;

/// <summary>
/// A single menu entry. Set <paramref name="Href"/> to render a real link
/// instead of a button, which keeps navigation menus usable with middle-click,
/// "open in new tab" and browser history.
/// </summary>
public sealed record MenuItem(
    string Id,
    string Label,
    string? Description = null,
    RenderFragment? Icon = null,
    bool Disabled = false,
    string? Href = null,
    string? Target = null,
    bool Visible = true);

/// <summary>A group of menu entries with a collapsible disclosure header.</summary>
public sealed record MenuGroup(
    string Key,
    string Label,
    IReadOnlyList<MenuItem> Items,
    RenderFragment? Icon = null,
    bool InitiallyOpen = true,
    bool Disabled = false,
    bool Visible = true);

[JsModule("Components/Menu/Menu.razor.js", Name = "menu", Interactive = true)]
public partial class Menu : AeterniComponent
{
    private const string ModuleName = "menu";

    /// <summary>Item slot value used when the focused node is a group toggle.</summary>
    private const int NoItem = -1;

    /// <summary>Sentinel for "no menu node has been focused yet".</summary>
    private const int NothingFocused = -1;

    [Parameter, EditorRequired] public IReadOnlyList<MenuGroup> Items { get; set; } = [];

    [Parameter] public string? SelectedId { get; set; }

    [Parameter] public bool Accordion { get; set; }

    /// <summary>
    /// Controlled expansion state. When set, the consumer owns which groups are
    /// open and receives every change through <see cref="OpenKeysChanged"/>;
    /// when null the component keeps the state itself.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? OpenKeys { get; set; }

    [Parameter] public string? AriaLabel { get; set; }

    [Parameter] public EventCallback<string> SelectedIdChanged { get; set; }

    [Parameter] public EventCallback<IReadOnlyList<string>> OpenKeysChanged { get; set; }

    [Parameter] public EventCallback<MenuItem> OnItemSelected { get; set; }

    [Parameter] public EventCallback<MenuGroup> OnGroupToggled { get; set; }

    private readonly HashSet<string> _open = [];
    private bool _initialized;
    private int _focusedGroupIndex = NothingFocused;
    private int _focusedItemIndex = NothingFocused;

    private bool IsControlled => OpenKeys is not null;

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-menu").Add("is-accordion", Accordion);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!IsControlled)
        {
            if (!_initialized)
            {
                _initialized = true;

                // Accordion keeps a single group open, so only the first requested
                // group is expanded on load; the selected group opens below.
                foreach (var group in Items.Where(group => group.Visible && group.InitiallyOpen).Take(Accordion ? 1 : int.MaxValue))
                {
                    _open.Add(group.Key);
                }
            }

            PruneOpenGroups();
            OpenSelectedGroup();
        }
    }

    internal bool IsOpen(string key) => IsControlled ? OpenKeys!.Contains(key) : _open.Contains(key);

    internal bool IsGroupDisabled(MenuGroup group) => Disabled || group.Disabled;

    internal bool IsItemDisabled(MenuGroup group, MenuItem item) => Disabled || group.Disabled || item.Disabled;

    internal bool IsSelected(MenuItem item) => string.Equals(SelectedId, item.Id, StringComparison.Ordinal);

    /// <summary>Reverse-tabnabbing guard for links opened in a new context.</summary>
    internal static string? LinkRel(MenuItem item) =>
        string.Equals(item.Target, "_blank", StringComparison.OrdinalIgnoreCase) ? "noopener noreferrer" : null;

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

    private void OpenSelectedGroup()
    {
        if (string.IsNullOrWhiteSpace(SelectedId))
        {
            return;
        }

        foreach (var group in Items.Where(group => group.Visible && group.Items.Any(item => item.Id == SelectedId)))
        {
            _open.Add(group.Key);
            if (Accordion) _open.RemoveWhere(key => key != group.Key);
        }
    }

    private string GroupToggleId(int groupIndex) => $"{ElementId}-group-{groupIndex}";

    private string GroupRegionId(int groupIndex) => $"{ElementId}-group-{groupIndex}-items";

    private string ItemId(int groupIndex, int itemIndex) => $"{ElementId}-group-{groupIndex}-item-{itemIndex}";

    private void TrackFocus(int groupIndex, int itemIndex)
    {
        _focusedGroupIndex = groupIndex;
        _focusedItemIndex = itemIndex;
    }

    private async Task ToggleGroupAsync(MenuGroup group)
    {
        if (IsGroupDisabled(group))
        {
            return;
        }

        var next = new HashSet<string>(IsControlled ? OpenKeys! : _open, StringComparer.Ordinal);
        if (next.Contains(group.Key))
        {
            next.Remove(group.Key);
        }
        else
        {
            if (Accordion) next.Clear();
            next.Add(group.Key);
        }

        if (IsControlled)
        {
            await OpenKeysChanged.InvokeAsync(next.ToArray());
        }
        else
        {
            _open.Clear();
            _open.UnionWith(next);
        }

        await OnGroupToggled.InvokeAsync(group);
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectAsync(MenuGroup group, MenuItem item)
    {
        if (IsItemDisabled(group, item))
        {
            return;
        }

        await SelectedIdChanged.InvokeAsync(item.Id);
        await OnItemSelected.InvokeAsync(item);
    }

    /// <summary>Wires the module up with the root element so it can suppress the
    /// browser's default arrow-key scrolling and report the writing direction.</summary>
    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RootElementInitialized)
        {
            await JsModuleManager.InvokeModuleVoidAsync(ModuleName, "attach", InstanceId, RootElement);
        }
    }

    /// <summary>
    /// Keyboard model for a disclosure navigation menu: the links and buttons
    /// stay in the tab order, and the arrow keys additionally move focus between
    /// the nodes that are currently reachable (group toggles plus the items of
    /// open groups). Enter and Space keep their native behaviour, so links
    /// activate like links and buttons like buttons.
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
            case "ArrowLeft":
                if (await IsExpandKeyAsync(args.Key))
                {
                    await ExpandFocusedGroupAsync();
                }
                else
                {
                    await CollapseFocusedGroupAsync(nodes);
                }

                break;
        }
    }

    /// <summary>
    /// Maps a horizontal key to "expand" in the current writing direction, so
    /// ArrowRight expands in LTR and ArrowLeft does in RTL.
    /// </summary>
    private async Task<bool> IsExpandKeyAsync(string key)
    {
        var isRight = string.Equals(key, "ArrowRight", StringComparison.Ordinal);
        // Returns false (LTR) when the module is unavailable, which keeps
        // server-side or JS-less rendering working.
        var isRtl = await JsModuleManager.InvokeModuleAsync<bool>(ModuleName, "isRtl", InstanceId);
        return isRight != isRtl;
    }

    /// <summary>
    /// Lists the nodes reachable with the arrow keys in render order. Hidden and
    /// disabled nodes are skipped because they are not focusable either.
    /// </summary>
    private List<(int GroupIndex, int ItemIndex)> BuildVisibleNodes()
    {
        var nodes = new List<(int GroupIndex, int ItemIndex)>();

        for (var groupIndex = 0; groupIndex < Items.Count; groupIndex++)
        {
            var group = Items[groupIndex];
            if (!group.Visible)
            {
                continue;
            }

            if (!IsGroupDisabled(group))
            {
                nodes.Add((groupIndex, NoItem));
            }

            if (!IsOpen(group.Key))
            {
                continue;
            }

            for (var itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
            {
                var item = group.Items[itemIndex];
                if (item.Visible && !IsItemDisabled(group, item))
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

        await ToggleGroupAsync(group);
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
            // Inside an expanded group, the collapse key returns focus to its toggle.
            var toggleIndex = nodes.FindIndex(node => node == (_focusedGroupIndex, NoItem));
            await FocusNodeAsync(nodes, toggleIndex);
            return;
        }

        if (IsOpen(group.Key))
        {
            await ToggleGroupAsync(group);
        }
    }
}
