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
}

/// <summary>
/// Single source of truth for the showcase sidebar: the routing entries of
/// /components and the component counters derived from them.
/// </summary>
public static class ShowcaseCatalog
{
    public static IReadOnlyList<ShowcaseNavGroup> Groups { get; } =
    [
        new("开始", FontAwesomeIcons.Solid.Rocket, [
            new("install", "安装", "安装与初始化") { IsComponent = false }
        ]),
        new("设计基础", FontAwesomeIcons.Solid.Palette, [
            new("colors", "色彩与语义", "色族形态对照") { IsComponent = false }
        ]),
        new("基础组件", FontAwesomeIcons.Solid.Cubes, [
            new("button", "Button", "动作按钮"),
            new("button-group", "ButtonGroup", "按钮组合"),
            new("surface", "Surface", "视觉容器"),
            new("card", "Card", "结构化容器"),
            new("divider", "Divider", "分隔线"),
            new("empty", "Empty", "空状态占位"),
            new("icon", "Icon", "SVG 图标"),
            new("tag", "Tag", "标签状态"),
            new("badge", "Badge", "数字与圆点角标")
        ]),
        new("表单组件", FontAwesomeIcons.Solid.Keyboard, [
            new("input", "Input", "单行输入"),
            new("textarea", "Textarea", "多行输入"),
            new("radio", "Radio / RadioGroup", "单选分组"),
            new("form-field", "FormField", "字段组合"),
            new("label", "Label", "原生标签"),
            new("checkbox", "Checkbox", "复选框"),
            new("switch", "Switch", "二态开关"),
            new("segmented", "Segmented", "分段单选"),
            new("rating", "Rating", "整数评分"),
            new("combobox", "ComboBox", "下拉选择")
        ]),
        new("导航组件", FontAwesomeIcons.Solid.Compass, [
            new("list", "List / ListItem", "列表选择"),
            new("menu", "Menu", "分组菜单"),
            new("tabs", "Tabs", "标签页导航")
        ]),
        new("状态反馈", FontAwesomeIcons.Solid.BarsProgress, [
            new("progress", "Progress", "进度展示"),
            new("spinner", "Spinner", "加载指示器"),
            new("skeleton", "Skeleton", "内容占位")
        ]),
        new("通知组件", FontAwesomeIcons.Solid.Bell, [
            new("popup", "Popup / Popover", "基础浮层"),
            new("tooltip", "Tooltip", "辅助提示"),
            new("feedback", "Dialog / Toast", "反馈与浮层"),
            new("drawer", "Drawer", "边缘滑出面板")
        ]),
        new("主题组件", FontAwesomeIcons.Solid.Swatchbook, [
            new("theme", "Theme", "主题模式")
        ])
    ];

    /// <summary>Number of component entries, excluding non-component pages.</summary>
    public static int ComponentCount => Groups
        .SelectMany(group => group.Items)
        .Count(item => item.IsComponent);

    /// <summary>Number of showcase groups (categories).</summary>
    public static int GroupCount => Groups.Count;

    /// <summary>Hosted application types covered by the library.</summary>
    public const int HostCount = 3;

    /// <summary>Supported theme modes: System / Light / Dark.</summary>
    public const int ThemeModeCount = 3;
}
