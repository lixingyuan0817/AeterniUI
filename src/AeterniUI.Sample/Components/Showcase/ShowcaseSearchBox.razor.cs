using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 头部常驻搜索框：搜索页面与页内段，命中后就地导航到该段。
/// </summary>
/// <remarks>
/// 数据来自生成物 <see cref="ShowcaseSearchIndex"/>（目录条目 + 页面上真实存在的段）。过滤、
/// 活动项、ARIA 与键盘语义全在 C#，模型照库内 <c>Autocomplete</c> / <c>ComboBox</c>：输入框是
/// <c>combobox</c>、结果容器是 <c>listbox</c>、活动项用 <c>aria-activedescendant</c>（条目本身
/// 不进 Tab 序列）、方向键循环、Home/End 到首尾、Enter 打开、Escape 交给 Popover 关掉下拉
/// 但保留输入内容，点击外部同理。
/// DOM 侧只剩两件 Blazor 做不到的事，见 <c>wwwroot/js/sample-search.js</c>：导航键的默认动作
/// 与活动项滚动，以及跨页跳转后滚到目标锚点。
/// </remarks>
public partial class ShowcaseSearchBox : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./js/sample-search.js";

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    private readonly List<ShowcaseSearchEntry> _matches = [];

    private IJSObjectReference? _module;
    private ElementReference _inputElement;
    private string _query = string.Empty;
    private string? _pendingAnchor;
    private bool _open;
    private int _activeIndex;

    private string InputId => "showcase-header-search";

    private string ListboxId => "showcase-header-search-listbox";

    private string? ActiveOptionId => _activeIndex >= 0 && _activeIndex < _matches.Count
        ? OptionId(_activeIndex)
        : null;

    /// <summary>
    /// 输入框上的 combobox 契约。`Input` 自己会写 aria-label 与字段状态，这几个键由调用方补，
    /// 与库内 Autocomplete 传给自己的 `Input` 的那一组一致。
    /// </summary>
    private IReadOnlyDictionary<string, object> InputAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = "combobox",
                ["aria-expanded"] = _open ? "true" : "false",
                ["aria-controls"] = ListboxId,
                ["aria-haspopup"] = "listbox",
                ["aria-autocomplete"] = "list"
            };

            if (ActiveOptionId is not null)
            {
                attributes["aria-activedescendant"] = ActiveOptionId;
            }

            return attributes;
        }
    }

    protected override void OnInitialized() =>
        // 跨页导航带着段锚点时框架不会自己滚动（它只在同文档内滚动），落地后由这里补一次。
        NavigationManager.LocationChanged += HandleLocationChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }

        if (_module is not null && _open)
        {
            await _module.InvokeVoidAsync("sync", _inputElement);
        }
    }

    private Task HandleQueryChangedAsync(string? value)
    {
        _query = value ?? string.Empty;
        // 查询一变，第一条命中就是新的活动项：不从上一个查询的结果上接着走。
        _activeIndex = 0;
        RefreshMatches();
        // 空查询不展开：一进站就弹一个面板是打扰，而且列表里那一屏结果对读者没有指向性。
        _open = _matches.Count > 0 || !string.IsNullOrWhiteSpace(_query);
        return Task.CompletedTask;
    }

    private Task HandlePopoverOpenChangedAsync(bool open)
    {
        // Popover 只在 Escape、点击外部与滚动时请求关闭；输入内容与查询结果都保留，
        // 读者可以接着改关键词（否则一次误触就丢了输入）。
        _open = open;
        if (!open)
        {
            _activeIndex = 0;
        }

        return Task.CompletedTask;
    }

    /// <summary>打开命中项：清空控件（它已经完成使命）再导航到该段的锚点。</summary>
    private void Navigate(ShowcaseSearchEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _open = false;
        _query = string.Empty;
        _activeIndex = 0;
        _matches.Clear();
        _pendingAnchor = entry.SectionId;
        NavigationManager.NavigateTo(entry.Href);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var anchor = _pendingAnchor;
        _pendingAnchor = null;
        if (anchor is null || _module is null)
        {
            return;
        }

        _ = ScrollToAnchorAsync(anchor);
    }

    private async Task ScrollToAnchorAsync(string anchor)
    {
        try
        {
            await _module!.InvokeAsync<bool>("scrollToAnchor", anchor);
        }
        catch (JSDisconnectedException)
        {
            // 运行时已卸载，没什么可滚的了。
        }
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        // 带修饰键的按键留给浏览器与上层，这里只处理裸键。
        if (args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return Task.CompletedTask;
        }

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
                Navigate(ActiveEntry());
                break;
        }

        return Task.CompletedTask;
    }

    private ShowcaseSearchEntry? ActiveEntry() =>
        _activeIndex >= 0 && _activeIndex < _matches.Count ? _matches[_activeIndex] : null;

    private void MoveActive(int delta)
    {
        if (_matches.Count == 0)
        {
            _activeIndex = -1;
            return;
        }

        // 下拉已经被 Escape 收起、但输入内容还在时，方向键把它重新拉出来。
        if (!_open)
        {
            _open = true;
            _activeIndex = 0;
            return;
        }

        _activeIndex = _activeIndex < 0
            ? 0
            : (_activeIndex + delta + _matches.Count) % _matches.Count;
    }

    private void SetActive(int index) =>
        _activeIndex = _matches.Count == 0 ? -1 : Math.Clamp(index, 0, _matches.Count - 1);

    /// <summary>
    /// 命中项：段标题、页标题、页面 id 与分类都参与匹配，大小写不敏感；排序按"命中在哪"分层
    /// （段标题 → 页标题 → 页 id → 分类），同层保持索引顺序，因此同一查询的结果顺序稳定。
    /// </summary>
    private void RefreshMatches()
    {
        var query = _query.Trim();
        _matches.Clear();

        if (query.Length == 0)
        {
            _activeIndex = -1;
            return;
        }

        _matches.AddRange(ShowcaseSearchIndex.Entries
            .Select((entry, index) => (entry, index, rank: RankOf(entry, query)))
            .Where(candidate => candidate.rank >= 0)
            .OrderBy(candidate => candidate.rank)
            .ThenBy(candidate => candidate.index)
            .Select(candidate => candidate.entry));

        _activeIndex = _matches.Count == 0 ? -1 : 0;
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

    /// <summary>页面条目的段标题就是页标题，因此这里只补分类，不重复一遍页名。</summary>
    private static string MetaOf(ShowcaseSearchEntry entry) =>
        string.Equals(entry.SectionTitle, entry.PageTitle, StringComparison.Ordinal)
            ? entry.Category
            : $"{entry.PageTitle} · {entry.Category}";

    private static string OptionId(int index) => $"showcase-header-search-option-{index}";

    private string OptionClass(int index) =>
        index == _activeIndex ? "showcase-search-box__option is-active" : "showcase-search-box__option";

    private void HandleInputElementChanged(ElementReference element) => _inputElement = element;

    public async ValueTask DisposeAsync()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;

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
