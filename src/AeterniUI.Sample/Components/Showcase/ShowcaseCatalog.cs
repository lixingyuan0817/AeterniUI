namespace AeterniUI.Sample.Components.Showcase;

/// <summary>A component group shown in the showcase sidebar.</summary>
public sealed record ShowcaseNavGroup(string Label, IReadOnlyList<ShowcaseNavItem> Items)
{
    public int Count => Items.Count;
}

/// <summary>A single showcase entry: id anchors the preview section on /components.</summary>
public sealed record ShowcaseNavItem(string Id, string Label, string Summary);

/// <summary>
/// Single source of truth for the showcase navigation and the stats shown on
/// the docs home page (component/group counts stay in sync automatically).
/// </summary>
public static class ShowcaseCatalog
{
    public static IReadOnlyList<ShowcaseNavGroup> Groups { get; } =
    [
        new("基础", [
            new("button", "Button", "动作按钮"),
            new("button-group", "ButtonGroup", "按钮组合"),
            new("surface", "Surface", "视觉容器"),
            new("card", "Card", "结构化容器"),
            new("icon", "Icon", "SVG 图标")
        ]),
        new("表单", [
            new("input", "Input", "单行输入"),
            new("textarea", "Textarea", "多行输入"),
            new("radio", "Radio / RadioGroup", "单选分组"),
            new("progress", "Progress", "进度展示"),
            new("form-field", "FormField", "字段组合"),
            new("label", "Label", "原生标签"),
            new("checkbox", "Checkbox", "复选框"),
            new("switch", "Switch", "二态开关")
        ]),
        new("选择与状态", [
            new("tag", "Tag", "标签状态"),
            new("list", "List / ListItem", "列表选择"),
            new("rating", "Rating", "整数评分"),
            new("combobox", "ComboBox", "下拉选择")
        ]),
        new("系统反馈", [
            new("theme", "Theme", "主题模式"),
            new("popup", "Popup / Popover", "基础浮层"),
            new("tooltip", "Tooltip", "辅助提示"),
            new("feedback", "Dialog / Toast", "反馈与浮层")
        ])
    ];

    /// <summary>Number of showcase entries (components and combined demos).</summary>
    public static int ComponentCount => Groups.Sum(group => group.Count);

    /// <summary>Number of showcase groups (categories).</summary>
    public static int GroupCount => Groups.Count;

    /// <summary>Hosted application types covered by the library.</summary>
    public const int HostCount = 3;

    /// <summary>Supported theme modes: System / Light / Dark.</summary>
    public const int ThemeModeCount = 3;
}
