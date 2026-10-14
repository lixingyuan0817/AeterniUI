using AeterniUI.Enums;
using AeterniUI.Icons;
using AeterniUI.Icons.FontAwesome;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Pages;

/// <summary>
/// 「组件运营大屏」示例页（<c>/screen</c>）。它不新增任何公共 API：整屏由库已有组件的
/// 公共参数拼出来，页面自己只负责网格、间距与几处尺寸约定。
/// </summary>
/// <remarks>
/// 三处有意为之的取舍：
/// <list type="bullet">
/// <item>大屏假定深色。进入时把主题切到 Dark、离开时恢复进入前的模式；页面不直接写
/// <c>data-theme</c>，ThemeService 仍是唯一的主题驱动入口。</item>
/// <item>播放键与底部方块键只放大组件自己的尺寸变量（<c>--aeterni-icon-button-size</c>
/// 与 <c>--aeterni-icon-button-icon-size</c>），不引入第二套颜色、圆角或动效体系。</item>
/// <item>舞台中央是仓库里现成的示例图，换成真实素材只需要替换 <c>img</c> 的 <c>src</c>；
/// 页面上的文字全是组件库自己的语汇，没有照搬任何外部大屏的文案。</item>
/// </list>
/// </remarks>
public partial class Screen : ComponentBase, IDisposable
{
    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    private ThemeMode _previousMode = ThemeMode.System;
    private bool _forcedDark;

    private string _stamp = string.Empty;
    private string _nav = "release";
    private string _channel = "all";
    private string _view = "list";
    private bool _playing;
    private string _notice = "选一个快捷入口，这里给出即时反馈。";
    private IReadOnlyList<string> _openKeys = ["g1", "g2"];

    /// <summary>顶栏主导航。默认落在最后一项，和效果图里"当前应用"高亮的位置一致。</summary>
    private static readonly IReadOnlyList<NavEntry> NavEntries =
    [
        new("overview", "组件总览"),
        new("foundation", "设计基础"),
        new("theme", "主题配方"),
        new("release", "发布状态")
    ];

    /// <summary>分类切换（第一排 Segmented）。每一项自带左栏的两组树。</summary>
    private static readonly IReadOnlyList<ChannelEntry> Channels =
    [
        new("all", "全部组件",
        [
            new("g1", "基础组件", ["Button", "Card", "Surface", "Icon", "Tag", "Divider"]),
            new("g2", "表单组件", ["Input", "Textarea", "InputNumber", "Slider", "Checkbox", "Switch", "ComboBox"])
        ]),
        new("basic", "基础与容器",
        [
            new("g1", "容器与表面", ["Surface", "Card", "Divider", "Accordion"]),
            new("g2", "基础元素", ["Button", "IconButton", "Icon", "Tag", "Badge", "Avatar"])
        ]),
        new("form", "表单与选择",
        [
            new("g1", "输入", ["Input", "Textarea", "InputNumber", "Search", "Upload"]),
            new("g2", "选择与开关", ["Checkbox", "Switch", "Radio", "Segmented", "ComboBox", "MultiSelect"])
        ]),
        new("nav", "导航与展示",
        [
            new("g1", "导航", ["Menu", "Tabs", "Breadcrumb", "Stepper", "Pagination"]),
            new("g2", "数据展示", ["List", "VirtualList", "Descriptions", "Timeline"])
        ]),
        new("feedback", "反馈与占位",
        [
            new("g1", "反馈", ["Dialog", "Confirm", "Alert", "Toast", "Drawer"]),
            new("g2", "进度与占位", ["Progress", "Spinner", "Skeleton", "Empty"])
        ])
    ];

    /// <summary>视图切换（第二排 Segmented）：只换舞台标题与左栏读法。</summary>
    private static readonly IReadOnlyList<ViewEntry> Views =
    [
        new("list", "清单视图"),
        new("metrics", "度量视图")
    ];

    /// <summary>底部快捷入口：图标方块加一行标签，整条占一个 Toolbar 停留点。</summary>
    private static readonly IReadOnlyList<DockEntry> DockEntries =
    [
        new("组件库", FontAwesomeIcons.Solid.Cubes, ButtonIntent.Default),
        new("设计令牌", FontAwesomeIcons.Solid.Palette, ButtonIntent.Default),
        new("度量", FontAwesomeIcons.Solid.ChartPie, ButtonIntent.Default),
        new("布局", FontAwesomeIcons.Solid.LayerGroup, ButtonIntent.Neutral),
        new("表单", FontAwesomeIcons.Solid.Sliders, ButtonIntent.Neutral)
    ];

    private IReadOnlyList<NavEntry> NavList => NavEntries;

    private IReadOnlyList<string> ChannelValues => Channels.Select(entry => entry.Id).ToArray();

    private IReadOnlyList<string> ViewValues => Views.Select(entry => entry.Id).ToArray();

    private ChannelEntry CurrentChannel => Channels.First(entry => entry.Id == _channel);

    private string ChannelLabel => CurrentChannel.Label;

    private string ViewLabel => Views.First(entry => entry.Id == _view).Label;

    private string NavLabel => NavEntries.First(entry => entry.Id == _nav).Label;

    /// <summary>播放键的无障碍名。Razor 的属性值里不能直接嵌引号，所以文案在 C# 这边拼。</summary>
    private string PlayLabel => _playing ? "暂停演示回放" : "播放演示回放";

    private string TreeLabel => $"{ChannelLabel}分组";

    private int ItemCount => CurrentChannel.Groups.Sum(group => group.Lines.Count);

    private string ChannelLabelSelector(string id) => Channels.First(entry => entry.Id == id).Label;

    private string ViewLabelSelector(string id) => Views.First(entry => entry.Id == id).Label;

    protected override void OnInitialized()
    {
        _stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        _previousMode = ThemeService.Mode;
        ThemeService.SetMode(ThemeMode.Dark);
        _forcedDark = true;
    }

    public void Dispose()
    {
        if (!_forcedDark)
        {
            return;
        }

        ThemeService.SetMode(_previousMode);
        _forcedDark = false;
    }

    private Task SelectNavAsync(string id)
    {
        _nav = id;
        _notice = $"主导航已切到「{NavLabel}」。";
        return Task.CompletedTask;
    }

    private Task SelectChannelAsync(string id)
    {
        _channel = id;
        // 分组键在分类之间是共用的（g1 / g2），所以换分类时两栏仍然展开。
        _openKeys = ["g1", "g2"];
        _notice = $"分类已切到「{ChannelLabel}」，命中 {ItemCount} 个组件。";
        return Task.CompletedTask;
    }

    private Task SelectViewAsync(string id)
    {
        _view = id;
        _notice = $"视图已切到「{ViewLabel}」。";
        return Task.CompletedTask;
    }

    private Task SelectNextChannelAsync()
    {
        var index = 0;
        for (var i = 0; i < Channels.Count; i++)
        {
            if (Channels[i].Id == _channel)
            {
                index = i;
                break;
            }
        }

        return SelectChannelAsync(Channels[(index + 1) % Channels.Count].Id);
    }

    private Task SelectDockAsync(string label)
    {
        _notice = $"已触发快捷入口「{label}」。";
        return Task.CompletedTask;
    }

    private Task TogglePlayAsync()
    {
        _playing = !_playing;
        _notice = _playing ? "演示回放已开始。" : "演示回放已暂停。";
        return Task.CompletedTask;
    }

    private Task HandleOpenKeysChangedAsync(IReadOnlyList<string> keys)
    {
        _openKeys = keys;
        return Task.CompletedTask;
    }

    private sealed record NavEntry(string Id, string Label);

    private sealed record ChannelEntry(string Id, string Label, IReadOnlyList<TreeGroup> Groups);

    private sealed record TreeGroup(string Id, string Title, IReadOnlyList<string> Lines);

    private sealed record ViewEntry(string Id, string Label);

    private sealed record DockEntry(string Label, IconDefinition Icon, ButtonIntent Intent);
}
