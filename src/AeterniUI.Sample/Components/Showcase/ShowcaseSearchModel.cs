using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 页头搜索的共享状态：面板开合、查询、命中与活动项。
/// </summary>
/// <remarks>
/// 入口（页头里的触发器）与内容（居中面板）是两个组件：面板必须渲染在页头那张玻璃卡片之外，
/// 否则它自己的 <c>backdrop-filter</c> 只能采样卡片内部那张透明背景。两半需要一个共享状态把
/// 它们绑在一起——各持一份就会漂移成"触发器说开着、面板什么都没渲染"。
/// 状态在这里、DOM 侧只剩三件 Blazor 做不到的事（见 <c>wwwroot/js/sample-search.js</c>）：全局
/// 快捷键，打开时的滚动锁与焦点陷阱（复用库内 <c>aeterni_floating.js</c>，示例不另造一套），
/// 以及输入框上的导航键默认动作与活动项滚动；跨页跳转落到段锚点的滚动也在那里。
/// </remarks>
public sealed class ShowcaseSearchModel : IAsyncDisposable
{
    private const string ModulePath = "./js/sample-search.js";
    private static readonly ShowcaseSearchEntry[] NoMatches = [];

    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _jsRuntime;
    private readonly List<ShowcaseSearchEntry> _matches = [];
    private readonly List<ShowcaseSearchGroup> _groups = [];

    private IJSObjectReference? _module;
    private string? _pendingAnchor;
    private bool _restoreFocusOnClose = true;

    /// <summary>
    /// 同一次查询里按页面聚好的一组命中。组按页面首次命中的顺序排列，组内保持排序后的顺序；
    /// <see cref="Matches" /> 是这些组的顺序拼接，所以活动项下标与渲染顺序一一对应。
    /// </summary>
    public sealed record ShowcaseSearchGroup(
        string PageId,
        string PageTitle,
        string Category,
        IReadOnlyList<ShowcaseSearchEntry> Entries);

    public ShowcaseSearchModel(NavigationManager navigation, IJSRuntime jsRuntime)
    {
        _navigation = navigation;
        _jsRuntime = jsRuntime;
        // 跨页导航带着段锚点时框架不会自己滚动（它只在同文档内滚动），落地后由这里补一次。
        _navigation.LocationChanged += HandleLocationChanged;
    }

    /// <summary>状态变化后触发：页头触发器与面板两个视图据此重渲染。</summary>
    public event Action? Changed;

    /// <summary>触发器元素：面板关闭后焦点回到它。</summary>
    public ElementReference? Trigger { get; set; }

    /// <summary>面板是否打开。关着时面板整体不存在于 DOM 里。</summary>
    public bool Open { get; private set; }

    public string Query { get; private set; } = string.Empty;

    public IReadOnlyList<ShowcaseSearchEntry> Matches => _matches.Count == 0 ? NoMatches : _matches;

    /// <summary>
    /// 按页面分组后的命中：渲染端每组插一行页面标题，同页结果因此不会散落在别页之间。
    /// 组顺序就是 <see cref="Matches" /> 的顺序，键盘下标不需要另一套映射。
    /// </summary>
    public IReadOnlyList<ShowcaseSearchGroup> MatchGroups => _groups;

    /// <summary>当前查询的命中总数；面板用它显示结果计数。</summary>
    public int MatchCount => _matches.Count;

    public int ActiveIndex { get; private set; } = -1;

    public string? ActiveOptionId => ActiveIndex >= 0 && ActiveIndex < _matches.Count
        ? OptionId(ActiveIndex)
        : null;

    /// <summary>输入框 id：面板里的输入框与结果容器的 <c>aria-controls</c> 都指向它。</summary>
    public static string InputId => "showcase-header-search";

    public static string ListboxId => "showcase-header-search-listbox";

    public static string OptionId(int index) => $"showcase-header-search-option-{index}";

    /// <summary>页面分组行（role=group）的 id，由分组标题的 aria-labelledby 指向它。</summary>
    public static string GroupId(string pageId) => $"showcase-header-search-group-{pageId}";

    /// <summary>把触发器元素交给模型；元素没变时不打扰两个视图。</summary>
    public void SetTrigger(ElementReference trigger)
    {
        // `ElementReference` 没有相等运算符，按它的元素 id 比较：同一个元素重渲染后 id 不变。
        if (Trigger is { } current && string.Equals(current.Id, trigger.Id, StringComparison.Ordinal))
        {
            return;
        }

        Trigger = trigger;
        Notify();
    }

    /// <summary>打开面板：查询与命中从零开始，上一次的输入属于上一次的搜索。</summary>
    public void OpenPanel()
    {
        if (Open)
        {
            return;
        }

        Open = true;
        ResetQuery();
        Notify();
    }

    /// <summary>
    /// 关闭面板。<paramref name="restoreFocus" /> 为假时把焦点留给落地页——从结果跳走的场景里，
    /// 焦点回到页头触发器会和刚滚到的目标段脱节。
    /// </summary>
    public void ClosePanel(bool restoreFocus = true)
    {
        if (!Open)
        {
            return;
        }

        Open = false;
        _restoreFocusOnClose = restoreFocus;
        ResetQuery();
        Notify();
    }

    public void TogglePanel()
    {
        if (Open)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    /// <summary>查询一变，第一条命中就是新的活动项：不从上一个查询的结果上接着走。</summary>
    public void SetQuery(string? value)
    {
        Query = value ?? string.Empty;
        ActiveIndex = 0;
        RefreshMatches();
        Notify();
    }

    /// <summary>打开命中项：面板收起（它已经完成使命）再导航到该段的锚点。</summary>
    public void Pick(ShowcaseSearchEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _pendingAnchor = entry.SectionId;
        ClosePanel(restoreFocus: false);
        _navigation.NavigateTo(entry.Href);
    }

    public ShowcaseSearchEntry? ActiveEntry() =>
        ActiveIndex >= 0 && ActiveIndex < _matches.Count ? _matches[ActiveIndex] : null;

    public void MoveActive(int delta)
    {
        if (_matches.Count == 0)
        {
            ActiveIndex = -1;
            return;
        }

        ActiveIndex = ActiveIndex < 0
            ? 0
            : (ActiveIndex + delta + _matches.Count) % _matches.Count;
        Notify();
    }

    public void SetActive(int index)
    {
        ActiveIndex = _matches.Count == 0 ? -1 : Math.Clamp(index, 0, _matches.Count - 1);
        Notify();
    }

    /// <summary>输入框的键盘语义：带修饰键的按键留给浏览器与上层，这里只处理裸键。</summary>
    public void HandleKey(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowDown":
                MoveActive(1);
                break;

            case "ArrowUp":
                MoveActive(-1);
                break;

            case "Home":
                SetActive(0);
                break;

            case "End":
                SetActive(_matches.Count - 1);
                break;

            case "Enter":
                Pick(ActiveEntry());
                break;
        }
    }

    /// <summary>命中项的页码来源：段标题就是页标题时只补分类，不重复一遍页名。</summary>
    public static string MetaOf(ShowcaseSearchEntry entry) =>
        string.Equals(entry.SectionTitle, entry.PageTitle, StringComparison.Ordinal)
            ? entry.Category
            : $"{entry.PageTitle} · {entry.Category}";

    private void ResetQuery()
    {
        Query = string.Empty;
        ActiveIndex = -1;
        _matches.Clear();
        _groups.Clear();
    }

    /// <summary>
    /// 命中项：段标题、页标题、页面 id 与分类都参与匹配，大小写不敏感；排序按"命中在哪"分层
    /// （段标题 → 页标题 → 页 id → 分类），同层保持索引顺序，因此同一查询的结果顺序稳定。
    /// 排序后再按页面聚组（组按首次出现的页面顺序），最后把组按顺序摊平回 <c>_matches</c>：
    /// 渲染端的活动项下标、<c>aria-activedescendant</c> 与键盘移动因此仍然指向同一序列。
    /// </summary>
    private void RefreshMatches()
    {
        var query = Query.Trim();
        _matches.Clear();
        _groups.Clear();

        if (query.Length == 0)
        {
            ActiveIndex = -1;
            return;
        }

        var ordered = ShowcaseSearchIndex.Entries
            .Select((entry, index) => (entry, index, rank: RankOf(entry, query)))
            .Where(candidate => candidate.rank >= 0)
            .OrderBy(candidate => candidate.rank)
            .ThenBy(candidate => candidate.index)
            .Select(candidate => candidate.entry);

        var groupIndexByPage = new Dictionary<string, int>(StringComparer.Ordinal);
        var groupEntries = new List<List<ShowcaseSearchEntry>>();

        foreach (var entry in ordered)
        {
            if (!groupIndexByPage.TryGetValue(entry.PageId, out var groupIndex))
            {
                groupIndex = groupEntries.Count;
                groupIndexByPage[entry.PageId] = groupIndex;
                groupEntries.Add(new List<ShowcaseSearchEntry>());
            }

            groupEntries[groupIndex].Add(entry);
        }

        foreach (var entries in groupEntries)
        {
            var first = entries[0];
            _groups.Add(new ShowcaseSearchGroup(first.PageId, first.PageTitle, first.Category, entries));
            _matches.AddRange(entries);
        }

        ActiveIndex = _matches.Count == 0 ? -1 : 0;
    }

    private static int RankOf(ShowcaseSearchEntry entry, string query)
    {
        if (Contains(entry.SectionTitle, query)) return 0;
        if (Contains(entry.PageTitle, query)) return 1;
        if (Contains(entry.PageId, query)) return 2;
        return Contains(entry.Category, query) ? 3 : -1;
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>挂上全局快捷键，并按平台把触发器上的键位提示写成 ⌘K / Ctrl K。</summary>
    public async Task AttachShortcutAsync(DotNetObjectReference<ShowcaseSearchBox> reference, ElementReference kbd)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await InvokeVoidAsync(() => module.InvokeVoidAsync("attachShortcut", reference, kbd));
    }

    public async Task DetachShortcutAsync()
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await InvokeVoidAsync(() => module.InvokeVoidAsync("detachShortcut"));
    }

    /// <summary>
    /// 面板进入 DOM 后的那一次互操作：滚动锁、焦点陷阱与输入框焦点。
    /// 输入框交给 JS 按 id 找（见 <see cref="InputId" />），因为父组件的渲染回调先于子组件执行，
    /// 面板第一次挂载时拿不到 <c>Input</c> 回填的元素引用。
    /// </summary>
    public async Task MountPanelAsync(ElementReference panel)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await InvokeVoidAsync(() => module.InvokeVoidAsync("mountPanel", panel, InputId, Trigger));
    }

    /// <summary>面板离开 DOM 后释放滚动锁与焦点陷阱，并按需把焦点还给触发器。</summary>
    public async Task UnmountPanelAsync()
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await InvokeVoidAsync(() => module.InvokeVoidAsync("unmountPanel", _restoreFocusOnClose));
    }

    /// <summary>活动项滚动：条目用 <c>aria-activedescendant</c> 表达活动项，浏览器不会替它滚动。</summary>
    public async Task SyncActiveOptionAsync()
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await InvokeVoidAsync(() => module.InvokeVoidAsync("sync"));
    }

    private static async Task InvokeVoidAsync(Func<ValueTask> call)
    {
        try
        {
            await call();
        }
        catch (JSDisconnectedException)
        {
            // 运行时已卸载，没什么可同步的。
        }
    }

    private async Task<IJSObjectReference?> GetModuleAsync()
    {
        if (_module is not null)
        {
            return _module;
        }

        try
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }
        catch (JSDisconnectedException)
        {
            return null;
        }

        return _module;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var anchor = _pendingAnchor;
        _pendingAnchor = null;
        if (anchor is null)
        {
            return;
        }

        _ = ScrollToAnchorAsync(anchor);
    }

    private async Task ScrollToAnchorAsync(string anchor)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeAsync<bool>("scrollToAnchor", anchor);
        }
        catch (JSDisconnectedException)
        {
            // 运行时已卸载，没什么可滚的了。
        }
    }

    private void Notify() => Changed?.Invoke();

    public async ValueTask DisposeAsync()
    {
        _navigation.LocationChanged -= HandleLocationChanged;

        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detachShortcut");
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // 运行时已卸载，模块与监听随之消失。
        }
    }
}
