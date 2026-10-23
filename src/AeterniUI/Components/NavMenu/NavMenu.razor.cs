using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.NavMenu;

/// <summary>
/// One row in a <see cref="NavMenu"/>. Children turn the row into a disclosure
/// group: groups only expand and collapse, leaves are the selectable destinations.
/// </summary>
public sealed record NavMenuNode(
    string Id,
    string Label,
    IReadOnlyList<NavMenuNode>? Children = null,
    RenderFragment? Icon = null,
    string? Description = null,
    RenderFragment? Trailing = null,
    RenderFragment<NavMenuRowContext>? RowTemplate = null,
    string? Href = null,
    string? Target = null,
    bool Disabled = false,
    bool Visible = true,
    bool InitiallyOpen = true)
{
    /// <summary>Indicates whether the node renders as a disclosure group.</summary>
    public bool HasChildren => Children?.Any(child => child.Visible) == true;
}

/// <summary>
/// State passed to <see cref="NavMenuNode.RowTemplate"/>. The template renders
/// inside the row's link or button, so it must not contain its own action target;
/// put those in <see cref="NavMenuNode.Trailing"/>.
/// </summary>
public sealed record NavMenuRowContext(
    NavMenuNode Node,
    int Depth,
    bool IsExpanded,
    bool IsSelected,
    bool IsDisabled);

/// <summary>
/// Sidebar navigation menu: the disclosure/selection model of <c>Menu</c>, but
/// every row is a host template and groups have no built-in chevron. Groups expand
/// and collapse only; leaves are the rows that can be selected. If
/// <see cref="NavMenuNode.RowTemplate"/> is null the default row renders an icon,
/// a label and an optional description.
/// </summary>
[JsModule("Components/NavMenu/NavMenu.razor.js", Name = "navmenu", Interactive = true)]
public partial class NavMenu : AeterniComponent
{
    private const string ModuleName = "navmenu";

    /// <summary>Root nodes; <see cref="NavMenuNode.Children"/> nests to any depth.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<NavMenuNode> Items { get; set; } = [];

    /// <summary>受控的选中项 Id。只有叶子节点可以进入选中态（导航组只展开/折叠）。</summary>
    [Parameter]
    public string? SelectedId { get; set; }

    /// <summary>叶子节点被选中时触发。</summary>
    [Parameter]
    public EventCallback<string> SelectedIdChanged { get; set; }

    /// <summary>叶子节点被点击选中后触发，带回节点本身。</summary>
    [Parameter]
    public EventCallback<NavMenuNode> OnItemSelected { get; set; }

    /// <summary>受控的展开节点 Id 集合；未传入时由组件自己维护。</summary>
    [Parameter]
    public IReadOnlyList<string>? OpenKeys { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<string>> OpenKeysChanged { get; set; }

    /// <summary>导航组展开/折叠后触发。</summary>
    [Parameter]
    public EventCallback<NavMenuNode> OnGroupToggled { get; set; }

    /// <summary>每个层级最多展开一个导航组。</summary>
    [Parameter]
    public bool Accordion { get; set; }

    /// <summary>
    /// 行密度，默认 <see cref="NavMenuDensity.Default"/>：32px 行高、纵向 0 / 横向 8px 行内边距、
    /// 6px 行圆角、4px 列表间距、14px 字号与 <c>--aeterni-text-secondary</c> 行墨，与旧版观感逐项一致。
    /// <see cref="NavMenuDensity.Comfortable"/> 对齐工作台侧栏：38px 行高、3px 纵向内边距、13px 字号与
    /// <c>--aeterni-text-primary</c> 行墨；<see cref="NavMenuDensity.Compact"/> 用 28px 行高与 12px 字号。
    /// </summary>
    [Parameter]
    public NavMenuDensity Density { get; set; } = NavMenuDensity.Default;

    /// <summary>
    /// 图标槽尺寸，默认 <see cref="NavMenuIconSize.Default"/>（16px，<c>--aeterni-icon-size-md</c>）；
    /// <see cref="NavMenuIconSize.Small"/> 为 14px，<see cref="NavMenuIconSize.Large"/> 为 20px。
    /// 槽位通过 <c>--aeterni-icon-render-size</c> 把尺寸传给行模板里未显式指定尺寸的 <c>Icon</c>。
    /// </summary>
    [Parameter]
    public NavMenuIconSize IconSize { get; set; } = NavMenuIconSize.Default;

    /// <summary>
    /// 描述文字字号，默认 <see cref="NavMenuTextSize.Default"/>（11px，保持原有观感）；
    /// <see cref="NavMenuTextSize.Small"/> 为 10px，<see cref="NavMenuTextSize.Large"/> 为 12px。
    /// </summary>
    [Parameter]
    public NavMenuTextSize DescriptionSize { get; set; } = NavMenuTextSize.Default;

    /// <summary>
    /// 子级缩进，默认 <see cref="NavMenuChildIndent.Default"/>（引导线左侧 16px、内容 8px）；
    /// <see cref="NavMenuChildIndent.Compact"/> 为 12px / 4px，<see cref="NavMenuChildIndent.Spacious"/> 为 20px / 12px。
    /// </summary>
    [Parameter]
    public NavMenuChildIndent ChildIndent { get; set; } = NavMenuChildIndent.Default;

    /// <summary>导航地标的可访问名称；未提供时使用组件库文案表。</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private ElementReference _renderedRootElement;
    private readonly HashSet<string> _open = [];
    private readonly Dictionary<string, NavMenuNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _parents = new(StringComparer.Ordinal);
    private bool _initialized;
    private string? _focusedId;
    private List<NavMenuNode> _visible = [];

    /// <summary>
    /// The element this component rendered. REV-147: feeding the rendered node back
    /// keeps <c>Element</c> / <c>ElementChanged</c> alive, while the JS module still
    /// only receives the node this component owns.
    /// </summary>
    private ElementReference RenderedRootElement
    {
        get => _renderedRootElement;
        set
        {
            _renderedRootElement = value;
            RootElement = value;
        }
    }

    private bool IsControlled => OpenKeys is not null;

    private string EffectiveAriaLabel =>
        string.IsNullOrWhiteSpace(AriaLabel) ? UiText.NavMenuLabel : AriaLabel.Trim();

    protected override ClassBuilder BuildClass()
    {
        // The default tier of every option emits no modifier: the base sheet already
        // describes the pre-tier look, so only an actual departure adds a class.
        var builder = base.BuildClass().Add("aeterni-nav-menu");

        builder.Add(Density switch
        {
            NavMenuDensity.Compact => "aeterni-nav-menu--density-compact",
            NavMenuDensity.Comfortable => "aeterni-nav-menu--density-comfortable",
            _ => null
        });

        builder.Add(IconSize switch
        {
            NavMenuIconSize.Small => "aeterni-nav-menu--icon-small",
            NavMenuIconSize.Large => "aeterni-nav-menu--icon-large",
            _ => null
        });

        builder.Add(DescriptionSize switch
        {
            NavMenuTextSize.Small => "aeterni-nav-menu--description-small",
            NavMenuTextSize.Large => "aeterni-nav-menu--description-large",
            _ => null
        });

        builder.Add(ChildIndent switch
        {
            NavMenuChildIndent.Compact => "aeterni-nav-menu--indent-compact",
            NavMenuChildIndent.Spacious => "aeterni-nav-menu--indent-spacious",
            _ => null
        });

        return builder;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Density))
        {
            throw new ArgumentOutOfRangeException(nameof(Density), Density, "Unknown nav menu density.");
        }

        if (!Enum.IsDefined(IconSize))
        {
            throw new ArgumentOutOfRangeException(nameof(IconSize), IconSize, "Unknown nav menu icon size.");
        }

        if (!Enum.IsDefined(DescriptionSize))
        {
            throw new ArgumentOutOfRangeException(nameof(DescriptionSize), DescriptionSize, "Unknown nav menu description size.");
        }

        if (!Enum.IsDefined(ChildIndent))
        {
            throw new ArgumentOutOfRangeException(nameof(ChildIndent), ChildIndent, "Unknown nav menu child indent.");
        }

        ArgumentNullException.ThrowIfNull(Items);

        _nodes.Clear();
        _parents.Clear();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        void Collect(IEnumerable<NavMenuNode> nodes, string? parentId)
        {
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    throw new ArgumentException("NavMenu nodes must have a non-empty Id.", nameof(Items));
                }

                if (!ids.Add(node.Id))
                {
                    throw new ArgumentException($"NavMenu node id '{node.Id}' is duplicated.", nameof(Items));
                }

                _nodes[node.Id] = node;
                _parents[node.Id] = parentId;

                if (node.Children is not null)
                {
                    Collect(node.Children, node.Id);
                }
            }
        }

        Collect(Items, null);

        if (!IsControlled)
        {
            if (!_initialized)
            {
                _initialized = true;
                SeedInitialOpen(Items);
            }

            PruneOpenState();
            OpenSelectedPath();
        }

        RebuildVisibleNodes();
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !RootElementInitialized)
        {
            return;
        }

        await JsModuleManager.InvokeModuleVoidAsync(ModuleName, "attach", InstanceId, RenderedRootElement);
    }

    // ---------------------------------------------------------------- rendering helpers

    private bool IsDisabled(NavMenuNode node) => Disabled || node.Disabled;

    private bool IsSelected(NavMenuNode node) =>
        !node.HasChildren && string.Equals(SelectedId, node.Id, StringComparison.Ordinal);

    private bool IsOpen(string id) =>
        IsControlled ? OpenKeys!.Contains(id, StringComparer.Ordinal) : _open.Contains(id);

    /// <summary>Reverse-tabnabbing guard for links opened in a new context.</summary>
    internal static string? LinkRel(NavMenuNode node) =>
        string.Equals(node.Target, "_blank", StringComparison.OrdinalIgnoreCase) ? "noopener noreferrer" : null;

    private string NodeDomId(NavMenuNode node) => $"{ElementId}-node-{Sanitize(node.Id)}";

    private string RegionId(NavMenuNode node) => $"{ElementId}-group-{Sanitize(node.Id)}";

    private static string Sanitize(string id) => id.Replace(' ', '-');

    // ---------------------------------------------------------------- expansion model

    private void SeedInitialOpen(IReadOnlyList<NavMenuNode> nodes)
    {
        var openedAtLevel = false;
        foreach (var node in nodes)
        {
            if (!node.Visible || !node.HasChildren)
            {
                continue;
            }

            if (node.InitiallyOpen && !(Accordion && openedAtLevel))
            {
                _open.Add(node.Id);
                openedAtLevel = true;
            }

            SeedInitialOpen(node.Children!);
        }
    }

    private void PruneOpenState() =>
        _open.RemoveWhere(id => !_nodes.TryGetValue(id, out var node) || !node.HasChildren);

    private void OpenSelectedPath()
    {
        if (string.IsNullOrWhiteSpace(SelectedId) || !_nodes.TryGetValue(SelectedId, out var selected))
        {
            return;
        }

        // Groups are not selectable; a SelectedId pointing at one renders nothing.
        if (selected.HasChildren)
        {
            return;
        }

        for (var parent = _parents.GetValueOrDefault(SelectedId); parent is not null; parent = _parents.GetValueOrDefault(parent))
        {
            _open.Add(parent);
            if (Accordion)
            {
                CloseSiblings(parent);
            }
        }
    }

    private void CloseSiblings(string groupId)
    {
        if (!_parents.TryGetValue(groupId, out var parentId))
        {
            return;
        }

        var siblings = parentId is null
            ? Items
            : _nodes[parentId].Children ?? [];

        foreach (var sibling in siblings)
        {
            if (!string.Equals(sibling.Id, groupId, StringComparison.Ordinal))
            {
                _open.Remove(sibling.Id);
            }
        }
    }

    private void RebuildVisibleNodes()
    {
        var visible = new List<NavMenuNode>();

        void Visit(IEnumerable<NavMenuNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (!node.Visible)
                {
                    continue;
                }

                if (!IsDisabled(node))
                {
                    visible.Add(node);
                }

                if (node.HasChildren && IsOpen(node.Id))
                {
                    Visit(node.Children!);
                }
            }
        }

        Visit(Items);
        _visible = visible;
    }

    // ---------------------------------------------------------------- activation

    private void TrackFocus(NavMenuNode node) => _focusedId = node.Id;

    private async Task ToggleAsync(NavMenuNode node)
    {
        if (!node.HasChildren || IsDisabled(node))
        {
            return;
        }

        var next = new HashSet<string>(IsControlled ? OpenKeys! : _open, StringComparer.Ordinal);
        if (!next.Add(node.Id))
        {
            next.Remove(node.Id);
            _open.Remove(node.Id);
        }
        else if (Accordion)
        {
            foreach (var sibling in SiblingsOf(node.Id))
            {
                next.Remove(sibling.Id);
            }
        }

        if (IsControlled)
        {
            await OpenKeysChanged.InvokeAsync(next.ToArray());
        }
        else
        {
            _open.Clear();
            _open.UnionWith(next);
            RebuildVisibleNodes();
        }

        await OnGroupToggled.InvokeAsync(node);
        await InvokeAsync(StateHasChanged);
    }

    private IEnumerable<NavMenuNode> SiblingsOf(string id)
    {
        if (!_parents.TryGetValue(id, out var parentId))
        {
            return [];
        }

        return parentId is null ? Items : _nodes[parentId].Children ?? [];
    }

    private async Task SelectAsync(NavMenuNode node)
    {
        if (node.HasChildren || IsDisabled(node))
        {
            return;
        }

        await SelectedIdChanged.InvokeAsync(node.Id);
        await OnItemSelected.InvokeAsync(node);
    }

    // ---------------------------------------------------------------- keyboard model

    /// <summary>
    /// Disclosure-navigation keyboard model: every link and toggle stays in the
    /// native tab order, and the arrow keys additionally move focus between the
    /// reachable rows. Enter/Space keep their native behaviour.
    /// </summary>
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (_visible.Count == 0)
        {
            return;
        }

        var index = _focusedId is null
            ? -1
            : _visible.FindIndex(node => string.Equals(node.Id, _focusedId, StringComparison.Ordinal));

        if (index < 0 && _focusedId is not null)
        {
            // Focus sat inside a group that was just collapsed: fall back to the
            // nearest still-visible ancestor instead of jumping to the first row.
            for (var parent = _parents.GetValueOrDefault(_focusedId); parent is not null; parent = _parents.GetValueOrDefault(parent))
            {
                index = _visible.FindIndex(node => string.Equals(node.Id, parent, StringComparison.Ordinal));
                if (index >= 0)
                {
                    break;
                }
            }
        }

        switch (args.Key)
        {
            case "ArrowDown":
                await FocusAsync(_visible[index < 0 ? 0 : (index + 1) % _visible.Count]);
                break;
            case "ArrowUp":
                await FocusAsync(_visible[index < 0 ? _visible.Count - 1 : (index - 1 + _visible.Count) % _visible.Count]);
                break;
            case "Home":
                await FocusAsync(_visible[0]);
                break;
            case "End":
                await FocusAsync(_visible[^1]);
                break;
            case "ArrowRight":
            case "ArrowLeft":
                var expand = await IsExpandKeyAsync(args.Key);
                await MoveNestedAsync(index < 0 ? null : _visible[index], expand);
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
        var isRtl = await JsModuleManager.InvokeModuleAsync<bool>(ModuleName, "isRtl", InstanceId);
        return isRight != isRtl;
    }

    private async Task MoveNestedAsync(NavMenuNode? node, bool expand)
    {
        if (node is null)
        {
            return;
        }

        if (node.HasChildren)
        {
            if (expand)
            {
                if (!IsOpen(node.Id))
                {
                    await ToggleAsync(node);
                    return;
                }

                var firstChild = node.Children!.FirstOrDefault(child => child.Visible && !IsDisabled(child));
                if (firstChild is not null)
                {
                    await FocusAsync(firstChild);
                }

                return;
            }

            if (IsOpen(node.Id))
            {
                await ToggleAsync(node);
                return;
            }

            await FocusParentAsync(node);
            return;
        }

        if (!expand)
        {
            await FocusParentAsync(node);
        }
    }

    private async Task FocusParentAsync(NavMenuNode node)
    {
        if (_parents.GetValueOrDefault(node.Id) is { } parentId && _nodes.TryGetValue(parentId, out var parent))
        {
            await FocusAsync(parent);
        }
    }

    private async Task FocusAsync(NavMenuNode node)
    {
        _focusedId = node.Id;
        await JsModuleManager.InvokeModuleVoidAsync(ModuleName, "focus", NodeDomId(node));
    }
}
