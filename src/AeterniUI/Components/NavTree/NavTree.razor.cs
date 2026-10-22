using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.NavTree;

/// <summary>
/// Accessible navigation tree for project and session navigation. Nodes can
/// carry a leading icon and a trailing fragment, so a trailing action menu is a
/// sibling of the node button instead of invalid nested interactive content.
/// </summary>
[JsModule("Components/NavTree/NavTree.razor.js", Name = "navtree", Interactive = true)]
public partial class NavTree : AeterniComponent
{
    /// <summary>根节点列表；节点按数据顺序渲染，子节点递归展开。</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<NavTreeNode> Items { get; set; } = [];

    /// <summary>
    /// 受控的选中节点 Id；未传入时由树自己维护选择。只有叶子节点会进入选中态：
    /// 可展开节点是导航组，只负责展开/折叠，不会渲染选中样式。
    /// </summary>
    [Parameter]
    public string? SelectedId { get; set; }

    /// <summary>叶子节点被选中时触发；可展开节点只切换展开状态，不触发该回调。</summary>
    [Parameter]
    public EventCallback<string> SelectedIdChanged { get; set; }

    /// <summary>受控的展开节点 Id 集合；未传入时由树自己维护展开状态。</summary>
    [Parameter]
    public IReadOnlyList<string>? OpenKeys { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<string>> OpenKeysChanged { get; set; }

    /// <summary>叶子节点被点击选中后触发；可展开节点只切换展开状态，不触发该回调。</summary>
    [Parameter]
    public EventCallback<NavTreeNode> OnNodeSelected { get; set; }

    /// <summary>树的可访问名称；未提供时使用组件库文案表。</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private ElementReference _root;
    private readonly HashSet<string> _open = [];
    private string? _internalSelectedId;
    private string? _focusedNodeId;
    private List<VisibleNode> _visibleNodes = [];
    private bool _attached;

    private bool IsControlledOpen => OpenKeys is not null;

    private string? EffectiveSelectedId => SelectedId ?? _internalSelectedId;

    private string EffectiveAriaLabel =>
        string.IsNullOrWhiteSpace(AriaLabel) ? UiText.NavTreeLabel : AriaLabel.Trim();

    private string? FirstVisibleNodeId => _visibleNodes.FirstOrDefault()?.Node.Id;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Items);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Flatten(Items))
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                throw new ArgumentException("NavTree nodes must have a non-empty Id.", nameof(Items));
            }

            if (!ids.Add(node.Id))
            {
                throw new ArgumentException($"NavTree node id '{node.Id}' is duplicated.", nameof(Items));
            }
        }

        if (!IsControlledOpen)
        {
            _open.RemoveWhere(id => !ids.Contains(id));
        }

        if (_internalSelectedId is not null && !ids.Contains(_internalSelectedId))
        {
            _internalSelectedId = null;
        }

        RebuildVisibleNodes();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-nav-tree");

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (_attached || string.IsNullOrEmpty(_root.Id))
        {
            return;
        }

        await JsModuleManager.InvokeModuleVoidAsync("navtree", "attach", InstanceId, _root);
        _attached = true;
    }

    private static IEnumerable<NavTreeNode> Flatten(IEnumerable<NavTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node.Children is null)
            {
                continue;
            }

            foreach (var child in Flatten(node.Children))
            {
                yield return child;
            }
        }
    }

    private void RebuildVisibleNodes()
    {
        var visible = new List<VisibleNode>();

        void Visit(IEnumerable<NavTreeNode> nodes, string? parentId, int depth)
        {
            foreach (var node in nodes)
            {
                if (!node.Visible)
                {
                    continue;
                }

                visible.Add(new VisibleNode(node, parentId, depth));
                if (node.HasChildren && IsOpen(node.Id))
                {
                    Visit(node.Children!, node.Id, depth + 1);
                }
            }
        }

        Visit(Items, null, 0);
        _visibleNodes = visible;
    }

    private bool IsOpen(string id) =>
        IsControlledOpen ? OpenKeys!.Contains(id, StringComparer.Ordinal) : _open.Contains(id);

    private bool IsSelected(string id) => string.Equals(EffectiveSelectedId, id, StringComparison.Ordinal);

    /// <summary>
    /// Expandable nodes are navigation groups: activating one only toggles its
    /// expansion, so a project header never looks like a destination. Selection
    /// belongs to leaves.
    /// </summary>
    private static bool CanSelect(NavTreeNode node) => !node.HasChildren;

    private bool IsNodeSelected(NavTreeNode node) => CanSelect(node) && IsSelected(node.Id);

    private string NodeElementId(string nodeId) =>
        $"{ElementId}-node-{nodeId.Replace(' ', '-')}";

    private string TabIndexFor(string nodeId)
    {
        // A host can point SelectedId at a group node; it never renders selected,
        // so it must not take the tree's single tab stop either.
        var selectedLeaf = _visibleNodes
            .FirstOrDefault(candidate => CanSelect(candidate.Node) && IsSelected(candidate.Node.Id))?.Node.Id;
        var active = _focusedNodeId ?? selectedLeaf ?? FirstVisibleNodeId;
        return string.Equals(active, nodeId, StringComparison.Ordinal) ? "0" : "-1";
    }

    private void HandleFocus(string nodeId) => _focusedNodeId = nodeId;

    private async Task ActivateAsync(NavTreeNode node)
    {
        if (node.Disabled)
        {
            return;
        }

        _focusedNodeId = node.Id;
        if (node.HasChildren)
        {
            // Navigation groups only expand/collapse; selecting them would give
            // the group row a destination look it does not have.
            await ToggleNodeAsync(node);
            return;
        }

        _internalSelectedId = node.Id;
        await SelectedIdChanged.InvokeAsync(node.Id);
        await OnNodeSelected.InvokeAsync(node);
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleNodeAsync(NavTreeNode node)
    {
        if (!node.HasChildren)
        {
            return;
        }

        var next = new HashSet<string>(IsControlledOpen ? OpenKeys! : _open, StringComparer.Ordinal);
        if (!next.Add(node.Id))
        {
            next.Remove(node.Id);
        }

        if (IsControlledOpen)
        {
            await OpenKeysChanged.InvokeAsync(next.ToArray());
        }
        else
        {
            _open.Clear();
            _open.UnionWith(next);
            RebuildVisibleNodes();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleKeyDownAsync(NavTreeNode node, KeyboardEventArgs args)
    {
        if (args.AltKey || args.CtrlKey || args.MetaKey)
        {
            return;
        }

        var current = _visibleNodes.FirstOrDefault(candidate => candidate.Node.Id == node.Id);
        var index = current is null ? -1 : _visibleNodes.IndexOf(current);
        switch (args.Key)
        {
            case "ArrowDown":
                await FocusNodeAtAsync(index < 0 ? 0 : index + 1);
                break;
            case "ArrowUp":
                await FocusNodeAtAsync(index < 0 ? _visibleNodes.Count - 1 : index - 1);
                break;
            case "Home":
                await FocusNodeAtAsync(0);
                break;
            case "End":
                await FocusNodeAtAsync(_visibleNodes.Count - 1);
                break;
            case "ArrowRight":
                if (node.HasChildren && !IsOpen(node.Id))
                {
                    await ToggleNodeAsync(node);
                }
                else if (node.HasChildren)
                {
                    var child = _visibleNodes.FirstOrDefault(candidate => candidate.ParentId == node.Id);
                    if (child is not null)
                    {
                        await FocusNodeAsync(child.Node.Id);
                    }
                }

                break;
            case "ArrowLeft":
                if (node.HasChildren && IsOpen(node.Id))
                {
                    await ToggleNodeAsync(node);
                }
                else
                {
                    var parent = _visibleNodes.FirstOrDefault(candidate => candidate.Node.Id == current?.ParentId);
                    if (parent is not null)
                    {
                        await FocusNodeAsync(parent.Node.Id);
                    }
                }

                break;
        }
    }

    private async Task FocusNodeAtAsync(int index)
    {
        if (index < 0 || index >= _visibleNodes.Count)
        {
            return;
        }

        await FocusNodeAsync(_visibleNodes[index].Node.Id);
    }

    private async Task FocusNodeAsync(string nodeId)
    {
        _focusedNodeId = nodeId;
        await InvokeAsync(StateHasChanged);
        await JsModuleManager.InvokeModuleVoidAsync("navtree", "focus", NodeElementId(nodeId));
    }

    private sealed record VisibleNode(NavTreeNode Node, string? ParentId, int Depth);
}
