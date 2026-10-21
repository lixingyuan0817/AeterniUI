using System.Globalization;
using AeterniUI.Components.Descriptions;
using AeterniUI.Components.Timeline;
using AeterniUI.Enums;
using AeterniUI.Icons;
using AeterniUI.Icons.FontAwesome;
using AeterniUI.Sample.Components.Showcase;
using AeterniUI.Services;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Pages;

/// <summary>
/// 「组件运营大屏」示例页（<c>/screen</c>）。全屏、不滚动、自适应视口，用库自己的组件
/// 与语义 Token 画一块持续更新的运行屏。
/// </summary>
/// <remarks>
/// 三条贯穿整页的约定：
/// <list type="bullet">
/// <item>数据分两类，页面上必须能分辨：库规模、分组占比、技术栈、组件样本来自
/// <see cref="ShowcaseCatalog"/> 的真实状态；波形、仪表与事件流是演示数据，各自带标签。</item>
/// <item>大屏假定深色。进入时把主题切到 Dark、离开时恢复进入前的模式，页面不直接写
/// <c>data-theme</c>。</item>
/// <item>动画只有一次 200ms 的「数据到位」，没有循环发光或漂浮。页面上的 Switch 与
/// 系统级 <c>prefers-reduced-motion</c> 都只关闭这段出场与数据过渡，不改变任何数值。</item>
/// </list>
/// </remarks>
public partial class Screen : ComponentBase, IDisposable
{
    private const int WavePoints = 36;
    private const int BarCount = 8;
    private const int MaxEvents = 3;
    private const int SampleCount = 6;

    /// <summary>刷新节拍：一秒一次，同时驱动时钟、波形、仪表、事件流与样本轮转。</summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    [Inject]
    private ThemeService ThemeService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private readonly Random _random = new(20261014);
    private readonly double[] _wave = new double[WavePoints];
    private readonly double[] _bars = new double[BarCount];
    private readonly double[] _gauges = [86, 72, 64, 91];
    private readonly List<TimelineItem> _events = [];

    private IReadOnlyList<RingSegment> _ring = [];
    private Timer? _pulse;
    private int _tick;
    private int _sampleIndex;
    private int _eventIndex;
    private bool _disposed;

    private ThemeMode _previousMode = ThemeMode.System;
    private bool _forcedDark;

    private DateTime _now = DateTime.Now;
    private string _nav = "overview";
    private string _range = "hour";
    private bool _motion = true;
    private string _notice = "点下方入口，查看真实反馈组件的行为。";

    private static readonly IReadOnlyList<ShowcaseNavGroup> Catalog = ShowcaseCatalog.Groups;

    private static readonly IReadOnlyList<NavEntry> NavEntries =
    [
        new("overview", "运行总览"),
        new("components", "组件清单"),
        new("tokens", "令牌与主题"),
        new("release", "构建与发布")
    ];

    private static readonly IReadOnlyList<RangeEntry> Ranges =
    [
        new("hour", "近一小时", 24, 1.0),
        new("day", "近一天", 15, 1.7),
        new("week", "近一周", 9, 2.6)
    ];

    private static readonly IReadOnlyList<DockEntry> DockEntries =
    [
        new("toast", "Toast", "已推送一条 Toast，不必打断当前操作。", FontAwesomeIcons.Solid.Bell, ButtonIntent.Default),
        new("alert", "Alert", "Alert 是模态提示，需要用户显式关闭。", FontAwesomeIcons.Solid.CircleInfo, ButtonIntent.Neutral),
        new("confirm", "Confirm", "Confirm 把用户的选择结果交回调用方。", FontAwesomeIcons.Solid.CircleQuestion, ButtonIntent.Neutral),
        new("clear", "清空事件", "事件流已清空。", FontAwesomeIcons.Solid.Trash, ButtonIntent.Danger)
    ];

    private static readonly string[] GaugeLabels = ["渲染", "契约", "文档", "令牌"];

    /// <summary>
    /// 分类色阶：品牌紫的 5 个明度档与 4 级中性灰交替，九个分组各占一档。
    /// 环形分段与图例共用这份映射；语义色不参与分类，只留给状态与异常。
    /// </summary>
    private static readonly string[] RingTones =
    [
        "brand-1", "neutral-1", "brand-2", "neutral-2", "brand-3",
        "neutral-3", "brand-4", "neutral-4", "brand-5"
    ];

    /// <summary>事件流种子。全库演进里的真实批次名，时间与顺序由页面上的时钟决定（演示数据）。</summary>
    private static readonly IReadOnlyList<EventSeed> EventSeeds =
    [
        new("契约检查通过", "根属性、ARIA 与焦点模型逐项比对完成", FontAwesomeIcons.Solid.CircleNodes, Color.Success),
        new("文档漂移检查通过", "retired 成员与文案表键没有出现未登记项", FontAwesomeIcons.Solid.ListCheck, Color.Success),
        new("令牌对比度通过", "色阶、品牌与文字墨三族同屏比对", FontAwesomeIcons.Solid.ChartPie, Color.Success),
        new("滚动条覆盖通过", "带 overflow 的容器都声明了自己那套滚动条", FontAwesomeIcons.Solid.BarsProgress, Color.Success),
        new("示例页构建完成", "示例宿主与静态站点同步产出", FontAwesomeIcons.Solid.ArrowsRotate, Color.Neutral),
        new("组件渲染完成", "玻璃面板与图标在深色下重新合成", FontAwesomeIcons.Solid.Microchip, Color.Neutral),
        new("主题切到深色", "大屏进入时请求深色，离开时恢复", FontAwesomeIcons.Solid.WandMagicSparkles, Color.Primary),
        new("审阅批次收口", "本批次的缺陷全部给出证据并归档", FontAwesomeIcons.Solid.Signal, Color.Neutral)
    ];

    private RangeEntry CurrentRange => Ranges.First(entry => entry.Id == _range);

    private string NavLabel => NavEntries.First(entry => entry.Id == _nav).Label;

    /// <summary>动画开关的 DOM 状态。写成属性而不是内联三元：Razor 的属性值里不能嵌引号。</summary>
    private string MotionState => _motion ? "on" : "off";

    /// <summary>样本墙当前项的类名，同样为了避开属性值里的引号。</summary>
    private string SampleClass(int index) => index == _sampleIndex ? "dash__sample is-current" : "dash__sample";

    private IReadOnlyList<string> RangeValues => Ranges.Select(entry => entry.Id).ToArray();

    private string RangeLabelSelector(string id) => Ranges.First(entry => entry.Id == id).Label;

    /// <summary>真实数据的四个读数：组件、宿主、主题模式与示例导航条目。</summary>
    private IReadOnlyList<MetricEntry> Metrics =>
    [
        new("组件", ShowcaseCatalog.ComponentCount.ToString(CultureInfo.InvariantCulture), $"{ShowcaseCatalog.GroupCount} 个分组"),
        new("宿主", ShowcaseCatalog.HostCount.ToString(CultureInfo.InvariantCulture), "Server / WASM"),
        new("主题模式", ShowcaseCatalog.ThemeModeCount.ToString(CultureInfo.InvariantCulture), "System / Light / Dark"),
        new("导航条目", Catalog.Sum(group => group.Count).ToString(CultureInfo.InvariantCulture), "示例工程侧栏")
    ];

    private IReadOnlyList<DescriptionItem> StackItems =>
    [
        new("目标框架", ".NET 10"),
        new("宿主模型", "Blazor Server / WebAssembly")
    ];

    private IReadOnlyList<TickerEntry> TickerItems => Catalog
        .Select(group => new TickerEntry(group.Label, group.Count))
        .ToArray();

    protected override void OnInitialized()
    {
        _previousMode = ThemeService.Mode;
        ThemeService.SetMode(ThemeMode.Dark);
        _forcedDark = true;

        SeedWave();
        SeedBars();
        BuildRing();
        SeedEvents();

        _pulse = new Timer(_ => _ = InvokeAsync(TickAsync), null, TickInterval, TickInterval);
    }

    public void Dispose()
    {
        _disposed = true;
        _pulse?.Dispose();
        _pulse = null;

        if (_forcedDark)
        {
            ThemeService.SetMode(_previousMode);
            _forcedDark = false;
        }
    }

    /// <summary>波形折线的 SVG points，viewBox 固定 300 × 60。</summary>
    private string WavePointsText()
    {
        var step = 300d / (WavePoints - 1);
        return string.Join(' ', _wave.Select((value, index) =>
            FormattableString.Invariant($"{index * step:0.##},{60 - value * 54:0.##}")));
    }

    /// <summary>波形下方的面积填充：沿折线走一圈再落到基线，交给 polygon 闭合。</summary>
    private string WaveAreaPointsText() => $"0,60 {WavePointsText()} 300,60";

    private static string PercentText(double value) =>
        FormattableString.Invariant($"{value:0}");

    private static string HeightText(double value) =>
        FormattableString.Invariant($"{value:0.##}%");

    private Task SelectNavAsync(string id)
    {
        _nav = id;
        _notice = $"视图已切到「{NavLabel}」。";
        return Task.CompletedTask;
    }

    private Task SelectRangeAsync(string id)
    {
        _range = id;
        _notice = $"波形时间窗已切到「{CurrentRange.Label}」。";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Switch 用 <c>@bind-Value</c> 绑定，值已经写回 <see cref="_motion"/>；
    /// 这里只在绑定完成后补一句状态文案，不重复写字段。
    /// </summary>
    private void HandleMotionAfterBind()
    {
        _notice = _motion ? "页面动画已开启：数据以短促阻尼过渡到位。" : "页面动画已关闭：数据直接到位。";
    }
    private async Task ClearEventsAsync()
    {
        _events.Clear();
        _notice = "事件流已清空。";
        await Task.CompletedTask;
    }

    private async Task RunDockAsync(DockEntry entry)
    {
        _notice = entry.Message;

        switch (entry.Id)
        {
            case "toast":
                DialogService.ShowToast(entry.Message);
                break;
            case "alert":
                await DialogService.AlertAsync(entry.Message);
                break;
            case "confirm":
                var accepted = await DialogService.ConfirmAsync("Confirm 的演示：继续会往事件流里写一条。");
                _notice = accepted ? "已确认，事件流已追加一条。" : "已取消，什么都没发生。";
                if (accepted)
                {
                    PushEvent();
                }

                break;
            default:
                await ClearEventsAsync();
                break;
        }
    }

    private Task TickAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _now = DateTime.Now;
        _tick++;

        PushWavePoint();

        if (_tick % 3 == 0)
        {
            DriftNumbers();
        }

        if (_tick % 4 == 0)
        {
            PushEvent();
        }

        if (_tick % 5 == 0)
        {
            RotateSamples();
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private void RotateSamples()
    {
        _sampleIndex = (_sampleIndex + 1) % SampleCount;
    }

    private void SeedWave()
    {
        for (var index = 0; index < WavePoints; index++)
        {
            _wave[index] = NextWaveValue(index);
        }
    }

    private void PushWavePoint()
    {
        Array.Copy(_wave, 1, _wave, 0, WavePoints - 1);
        _wave[^1] = NextWaveValue(_tick);
    }

    /// <summary>正弦底 + 噪声：时间窗只改振幅与周期，所以切档时波形立刻换形状。</summary>
    private double NextWaveValue(int index)
    {
        var range = CurrentRange;
        var phase = (index + _tick) / (double)WavePoints * Math.PI * range.Cycles;
        var noise = (_random.NextDouble() - 0.5) * 0.16;
        return Math.Clamp(0.5 + range.Amplitude / 100d * Math.Sin(phase) + noise, 0.06, 0.98);
    }

    private void SeedBars()
    {
        for (var index = 0; index < BarCount; index++)
        {
            _bars[index] = 34 + _random.NextDouble() * 58;
        }
    }

    /// <summary>演示数值的随机游走：仪表与柱高都留在合理区间内，不会飘成噪声。</summary>
    private void DriftNumbers()
    {
        for (var index = 0; index < _gauges.Length; index++)
        {
            _gauges[index] = Math.Clamp(_gauges[index] + (_random.NextDouble() - 0.5) * 5, 42, 99);
        }

        for (var index = 0; index < BarCount; index++)
        {
            _bars[index] = Math.Clamp(_bars[index] + (_random.NextDouble() - 0.5) * 20, 18, 96);
        }
    }

    /// <summary>
    /// 环形占比按真实分组条目数切段：段长按比例、段间留 2.4 个单位空隙，
    /// 每段自带 dasharray 与负向 dashoffset，从 12 点方向顺时针铺开。
    /// </summary>
    private void BuildRing()
    {
        const double radius = 74;
        const double gap = 2.4;
        var circumference = 2 * Math.PI * radius;
        var total = Math.Max(Catalog.Sum(group => group.Count), 1);
        var offset = 0d;
        var segments = new List<RingSegment>();
        var index = 0;

        foreach (var group in Catalog)
        {
            var ratio = group.Count / (double)total;
            var length = Math.Max(ratio * circumference - gap, 1.5);
            segments.Add(new RingSegment(
                group.Label,
                group.Count,
                RingTones[index % RingTones.Length],
                FormattableString.Invariant($"{length:0.##} {circumference - length:0.##}"),
                FormattableString.Invariant($"{-offset:0.##}")));
            offset += ratio * circumference;
            index++;
        }

        _ring = segments;
    }

    private void SeedEvents()
    {
        for (var index = 0; index < 4; index++)
        {
            PushEvent();
        }
    }

    private void PushEvent()
    {
        var seed = EventSeeds[_eventIndex % EventSeeds.Count];
        _eventIndex++;

        _events.Insert(0, new TimelineItem(
            seed.Title,
            seed.Description,
            TimeText: DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            Icon: seed.Icon,
            Color: seed.Color));

        if (_events.Count > MaxEvents)
        {
            _events.RemoveRange(MaxEvents, _events.Count - MaxEvents);
        }
    }

    private sealed record NavEntry(string Id, string Label);

    private sealed record RangeEntry(string Id, string Label, double Amplitude, double Cycles);

    private sealed record DockEntry(string Id, string Label, string Message, IconDefinition Icon, ButtonIntent Intent);

    private sealed record MetricEntry(string Label, string Value, string Note);

    private sealed record TickerEntry(string Label, int Count);
    private sealed record EventSeed(string Title, string Description, IconDefinition Icon, Color Color);

    private sealed record RingSegment(string Label, int Count, string Tone, string DashArray, string DashOffset);
}
