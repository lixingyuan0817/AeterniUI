using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 页头搜索的共享状态：查询、命中、活动项与开合状态。
/// </summary>
/// <remarks>
/// 输入框留在页头的玻璃卡片里，结果浮层必须渲染到卡片之外——玻璃卡片的
/// <c>backdrop-filter</c> 会成为最近的 backdrop root，浮层自己的模糊就只能采样那张卡片内部的
/// 透明区域，看起来完全没有模糊。所以这两半是两个组件，需要一个共享的状态源把它们绑在一起：
/// 各持一份就会漂移成"输入框说开着、浮层什么都没渲染"。
/// 状态在模型里、DOM 侧只剩两件 Blazor 做不到的事（见 <c>wwwroot/js/sample-search.js</c>）：导航
/// 键的默认动作与活动项滚动，以及跨页跳转后滚到目标锚点。
/// </remarks>
public sealed class ShowcaseSearchModel : IAsyncDisposable
{
    private const string ModulePath = "./js/sample-search.js";
    private static readonly ShowcaseSearchEntry[] NoMatches = [];

    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _jsRuntime;
    private readonly List<ShowcaseSearchEntry> _matches = [];

    private IJSObjectReference? _module;
    private string? _pendingAnchor;

    public ShowcaseSearchModel(NavigationManager navigation, IJSRuntime jsRuntime)
    {
        _navigation = navigation;
        _jsRuntime = jsRuntime;
        // 跨页导航带着段锚点时框架不会自己滚动（它只在同文档内滚动），落地后由这里补一次。
        _navigation.LocationChanged += HandleLocationChanged;
    }

    /// <summary>状态变化后触发：页头触发器与页面级浮层两个视图据此重渲染。</summary>
    public event Action? Changed;

    /// <summary>触发器包裹元素：浮层以它为锚点定位，也把它算作"层内"。</summary>
    public ElementReference? Anchor { get; set; }

    public string Query { get; private set; } = string.Empty;

    public bool Open { get; private set; }

    public IReadOnlyList<ShowcaseSearchEntry> Matches => _matches.Count == 0 ? NoMatches : _matches;

    public int ActiveIndex { get; private set; } = -1;

    public string? ActiveOptionId => ActiveIndex >= 0 && ActiveIndex < _matches.Count
        ? OptionId(ActiveIndex)
        : null;

    /// <summary>输入框 id：触发器的 <c>aria-controls</c> 与浮层里的容器都指向它。</summary>
    public static string InputId => "showcase-header-search";

    public static string ListboxId => "showcase-header-search-listbox";

    public static string OptionId(int index) => $"showcase-header-search-option-{index}";

    /// <summary>把锚点元素交给浮层；元素没变时不打扰两个视图。</summary>
    public void SetAnchor(ElementReference anchor)
    {
        // `ElementReference` 没有相等运算符，按它的元素 id 比较：同一个元素重渲染后 id 不变。
        if (Anchor is { } current && string.Equals(current.Id, anchor.Id, StringComparison.Ordinal))
        {
            return;
        }

        Anchor = anchor;
        Notify();
    }

    /// <summary>查询一变，第一条命中就是新的活动项：不从上一个查询的结果上接着走。</summary>
    public void SetQuery(string? value)
    {
        Query = value ?? string.Empty;
        ActiveIndex = 0;
        RefreshMatches();
        // 空查询不展开：一进站就弹一个面板是打扰，而且列表里那一屏结果对读者没有指向性。
        Open = _matches.Count > 0 || !string.IsNullOrWhiteSpace(Query);
        Notify();
    }

    /// <summary>Popover 只在 Escape、点击外部与滚动时请求关闭；输入内容与查询结果都保留。</summary>
    public void SetOpen(bool open)
    {
        Open = open;
        if (!open)
        {
            ActiveIndex = 0;
        }

        Notify();
    }

    /// <summary>打开命中项：清空控件（它已经完成使命）再导航到该段的锚点。</summary>
    public void Pick(ShowcaseSearchEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        Open = false;
        Query = string.Empty;
        ActiveIndex = 0;
        _matches.Clear();
        _pendingAnchor = entry.SectionId;
        Notify();
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

        // 下拉已经被 Escape 收起、但输入内容还在时，方向键把它重新拉出来。
        if (!Open)
        {
            Open = true;
            ActiveIndex = 0;
            Notify();
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

    /// <summary>
    /// 命中项：段标题、页标题、页面 id 与分类都参与匹配，大小写不敏感；排序按"命中在哪"分层
    /// （段标题 → 页标题 → 页 id → 分类），同层保持索引顺序，因此同一查询的结果顺序稳定。
    /// </summary>
    private void RefreshMatches()
    {
        var query = Query.Trim();
        _matches.Clear();

        if (query.Length == 0)
        {
            ActiveIndex = -1;
            return;
        }

        _matches.AddRange(ShowcaseSearchIndex.Entries
            .Select((entry, index) => (entry, index, rank: RankOf(entry, query)))
            .Where(candidate => candidate.rank >= 0)
            .OrderBy(candidate => candidate.rank)
            .ThenBy(candidate => candidate.index)
            .Select(candidate => candidate.entry));

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

    /// <summary>活动项滚动：条目用 <c>aria-activedescendant</c> 表达活动项，浏览器不会替它滚动。</summary>
    public async Task SyncActiveOptionAsync(ElementReference input)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("sync", input);
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
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // 运行时已卸载，模块与监听随之消失。
        }
    }
}
