using System.Reflection;
using AeterniUI.Icons;
using AeterniUI.Icons.FontAwesome;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>A component group shown in the showcase sidebar.</summary>
public sealed record ShowcaseNavGroup(string Label, IconDefinition Icon, IReadOnlyList<ShowcaseNavItem> Items)
{
    public int Count => Items.Count;
}

/// <summary>A single showcase entry: id anchors the preview section on /components.</summary>
/// <remarks>
/// Non-component pages (for example the installation guide) set
/// <see cref="IsComponent"/> to false so they stay out of <see cref="ShowcaseCatalog.ComponentCount"/>.
/// </remarks>
public sealed record ShowcaseNavItem(string Id, string Label, string Summary)
{
    public bool IsComponent { get; init; } = true;

    private string? _page;

    /// <summary>
    /// Page file under <c>Pages/Components</c> that renders this entry, without the
    /// extension. Derived from <see cref="Id"/> unless a page keeps a name that no
    /// longer matches its entry, in which case it is named here. The sidebar builds
    /// its "edit on GitHub" link from this value, so a wrong name produces a dead
    /// link instead of an obvious error — the sample gate asserts every entry's file
    /// exists.
    /// </summary>
    public string Page
    {
        get => _page ?? string.Concat(Id.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        init => _page = value;
    }
}

/// <summary>
/// Single source of truth for the showcase sidebar: the routing entries of
/// /components and the component counters derived from them.
/// </summary>
public static class ShowcaseCatalog
{
    public static IReadOnlyList<ShowcaseNavGroup> Groups { get; } =
    [
        new("开始", FontAwesomeIcons.Solid.Plug, [
            new("overview", "总览", "全部组件与分类索引") { IsComponent = false },
            new("install", "安装", "安装与初始化") { IsComponent = false }
        ]),
        new("设计基础", FontAwesomeIcons.Solid.Paintbrush, [
            new("colors", "色彩与语义", "色族、字体、圆角与阴影") { IsComponent = false }
        ]),
        new("基础组件", FontAwesomeIcons.Solid.Cubes, [
            new("button", "Button", "动作按钮"),
            new("icon-button", "IconButton", "图标按钮"),
            new("menu-button", "MenuButton", "菜单按钮"),
            new("icon-menu-button", "IconMenuButton", "图标菜单按钮"),
            new("split-button", "SplitButton", "分裂按钮"),
            new("button-group", "ButtonGroup", "按钮组合"),
            new("toolbar", "Toolbar", "单停留点动作工具栏"),
            new("toggle-group", "ToggleGroup", "单选与多选动作切换"),
            new("surface", "Surface", "视觉容器"),
            new("card", "Card", "结构化容器"),
            new("flash-card", "FlashCard", "图片闪卡与 3D 跟随"),
            new("flash-card-group", "FlashCardGroup", "抽卡式闪卡牌堆"),
            new("divider", "Divider", "分隔线"),
            new("splitter", "Splitter", "可拖动双栏布局"),
            new("empty", "Empty", "空状态占位"),
            new("icon", "Icon", "SVG 图标"),
            new("tag", "Tag", "标签状态"),
            new("badge", "Badge", "数字与圆点角标"),
            new("avatar", "Avatar", "头像与姓名缩写"),
            new("timeline", "Timeline", "按时间组织事件"),
            new("descriptions", "Descriptions", "标签与值的结构化展示"),
            new("accordion", "Accordion", "折叠面板")
        ]),
        new("表单组件", FontAwesomeIcons.Solid.Keyboard, [
            new("input", "Input", "单行输入"),
            new("textarea", "Textarea", "多行输入"),
            new("input-number", "InputNumber", "数值输入"),
            new("slider", "Slider", "单值滑块"),
            new("radio", "Radio", "Radio 与 RadioGroup 组合"),
            new("form-field", "FormField", "字段组合"),
            new("label", "Label", "原生标签"),
            new("checkbox", "Checkbox", "复选框"),
            new("switch", "Switch", "二态开关"),
            new("segmented", "Segmented", "分段单选"),
            new("rating", "Rating", "整数评分"),
            new("search", "Search", "关键词查询"),
            new("autocomplete", "Autocomplete", "输入建议"),
            new("multi-select", "MultiSelect", "多值选择"),
            new("upload", "Upload", "文件选择与拖放"),
            new("combobox", "ComboBox", "下拉选择"),
            new("cascader", "Cascader", "层级逐级选择"),
            new("transfer", "Transfer", "双列表迁移"),
            new("date-picker", "DatePicker", "日期与 DateRangePicker 范围"),
            new("time-picker", "TimePicker", "时间选择"),
            new("date-time-picker", "DateTimePicker", "日期与时间组合")
        ]),
        new("导航组件", FontAwesomeIcons.Solid.CompassDrafting, [
            new("breadcrumb", "Breadcrumb", "层级导航路径"),
            new("anchor", "Anchor", "页内锚点导航"),
            new("stepper", "Stepper", "线性流程步骤"),
            new("list", "List", "List 与 ListItem 组合选择"),
            new("virtual-list", "VirtualList", "大数据量虚拟列表"),
            new("menu", "Menu", "分组菜单"),
            new("nav-menu", "NavMenu", "项目/会话导航菜单"),
            new("tabs", "Tabs", "Tabs 与 Tab 组合导航"),
            new("pagination", "Pagination", "分页导航")
        ]),
        new("状态反馈", FontAwesomeIcons.Solid.ChartColumn, [
            new("progress", "Progress", "进度展示"),
            new("spinner", "Spinner", "加载指示器"),
            new("skeleton", "Skeleton", "内容占位")
        ]),
        new("通知组件", FontAwesomeIcons.Solid.Bell, [
            new("popup", "PopupHost", "PopupHost 与 Popover 浮层组合"),
            new("tooltip", "Tooltip", "辅助提示"),
            new("feedback", "Dialog", "Dialog、Confirm、Alert 与 Toast 服务"),
            new("form-dialog", "FormDialog", "表单弹窗与提交约定"),
            new("drawer", "Drawer", "边缘滑出面板")
        ]),
        new("主题组件", FontAwesomeIcons.Solid.WandMagicSparkles, [
            new("theme", "Theme", "ThemeProvider 与 ThemeSwitch 切换")
        ]),
        new("组合用法", FontAwesomeIcons.Solid.PuzzlePiece, [
            new("form-validation", "表单校验链", "字段、规则与提交的时序") { IsComponent = false },
            new("filter-toolbar", "筛选工具栏", "检索、筛选与空结果的出路") { IsComponent = false },
            new("settings-drawer", "设置抽屉", "抽屉里的设置、草稿与保存") { IsComponent = false },
            new("long-tasks", "通知与长任务", "进度、取消与结束通知") { IsComponent = false },
            new("data-list", "大数据列表", "800 条日志上的检索与批量动作") { IsComponent = false }
        ])
    ];

    private const string RepositoryUrl = "https://github.com/lixingyuan0817/AeterniUI";

    /// <summary>
    /// 库版本号，与 <c>Directory.Build.props</c> 的 <c>InformationalVersion</c> 同一来源：
    /// 从示例程序集的 informational version 读取，不在页面里再维护一份字符串。
    /// SourceLink 会在末尾追加 <c>+commit</c>，展示时去掉，只留 <c>10.35.0</c>。
    /// </summary>
    public static string Version { get; } = DisplayVersion();

    public static string ReleasesUrl => $"{RepositoryUrl}/releases";

    public static string IssuesUrl => $"{RepositoryUrl}/issues/new";

    public static string ContributorsUrl => $"{RepositoryUrl}/graphs/contributors";

    private static string DisplayVersion()
    {
        var informational = typeof(ShowcaseCatalog).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
        {
            return "0.0.0";
        }

        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }

    /// <summary>
    /// 条目 id → 路由。安装页挂在 <c>/components</c> 上而不是 <c>/components/install</c>，
    /// 因此这条规则只在这里写一次：侧栏导航与搜索结果都从这里取地址。
    /// </summary>
    public static string RouteFor(string id) => id == "install" ? "/components" : $"/components/{id}";

    /// <summary>
    /// GitHub 上该条目页面文件的编辑地址。
    /// </summary>
    /// <remarks>
    /// 由目录自己给出：链接原来挂在布局的页头行上，而页头行已经移进组件页容器内部（卡片要和
    /// 侧栏对齐），页面组件就直接从目录取这个地址。
    /// </remarks>
    public static string EditUrl(string id)
    {
        var item = Groups.SelectMany(group => group.Items).First(entry => entry.Id == id);
        return $"{RepositoryUrl}/blob/main/src/AeterniUI.Sample/Pages/Components/{item.Page}.razor";
    }

    /// <summary>Number of component entries, excluding non-component pages.</summary>
    public static int ComponentCount => Groups
        .SelectMany(group => group.Items)
        .Count(item => item.IsComponent);

    /// <summary>Number of showcase groups (categories).</summary>
    public static int GroupCount => Groups.Count;

    /// <summary>Hosted application types covered by the library.</summary>
    public const int HostCount = 2;

    /// <summary>Supported theme modes: System / Light / Dark.</summary>
    public const int ThemeModeCount = 3;
}
