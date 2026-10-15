using AeterniUI.Enums;
using AeterniUI.Icons;
using AeterniUI.Icons.FontAwesome;
using AeterniUI.Sample.Components.Showcase;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Sample.Pages;

/// <summary>
/// 首页（<c>/</c>）：库的展示入口。三段式——Hero（真实读数 + 入口）、常用组件墙
/// （每张卡里都是真组件，不是截图）、基础使用代码窗口。原先的提交时间线整块下线。
/// </summary>
/// <remarks>
/// 动画交给 GSAP（<c>wwwroot/js/vendor/gsap.min.js</c>，本地文件，不依赖 CDN）：
/// Hero 入场与数字滚动、卡片滚入视口揭示、悬停浮起与指针微倾、Hero 光斑跟随都在
/// <c>sample-home.js</c> 里；本页只负责首帧之后把根元素交出去、销毁时让 JS 侧回滚。
/// 系统开启 <c>prefers-reduced-motion</c> 时 JS 直接落到终态，页面不做特判。
/// </remarks>
public partial class Home : ComponentBase, IAsyncDisposable
{
    /// <summary>进度条的演示节拍：慢到不干扰阅读，又足以看出组件在动。</summary>
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(1500);

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private ElementReference _root;
    private IJSObjectReference? _module;
    private Timer? _progressPulse;
    private bool _disposed;

    // 组件墙里的演示状态。全部是真实双向绑定，不存在"画上去的假交互"。
    private bool _switchOn = true;
    private bool _checkboxOn;
    private string _name = string.Empty;
    private string _segment = "design";
    private double _slider = 62;
    private int _rating = 4;
    private double _progress = 38;

    private static readonly IReadOnlyList<string> SegmentValues = ["design", "build", "review"];

    private static readonly IReadOnlyList<ShowcaseGroup> Groups =
    [
        new("交互动作",
        [
            new("button", "button", "Button", "主要与次要动作"),
            new("icon-tooltip", "icon-tooltip", "IconButton + Tooltip", "图标动作与悬停说明"),
            new("switch", "switch", "Switch", "二态开关"),
            new("rating", "rating", "Rating", "整数评分")
        ]),
        new("表单输入",
        [
            new("input", "input", "Input", "单行文本"),
            new("checkbox", "checkbox", "Checkbox", "多选与确认"),
            new("segmented", "segmented", "Segmented", "分段单选"),
            new("slider", "slider", "Slider", "单值滑块")
        ]),
        new("展示与反馈",
        [
            new("tag-badge", "tag", "Tag + Badge", "标签与角标"),
            new("progress", "progress", "Progress", "进度与进度条"),
            new("avatar", "avatar", "Avatar + Spinner", "身份与等待"),
            new("feedback", "feedback", "Dialog / Alert / Confirm", "通知与确认")
        ])
    ];

    private string RangeLabelSelector(string id) => id switch
    {
        "design" => "设计",
        "build" => "构建",
        _ => "审阅"
    };

    private Task SelectSegmentAsync(string id)
    {
        _segment = id;
        return Task.CompletedTask;
    }

    private Task HandleNameChanged(string? value)
    {
        _name = value ?? string.Empty;
        return Task.CompletedTask;
    }

    protected override void OnInitialized() =>
        _progressPulse = new Timer(_ => _ = InvokeAsync(AdvanceProgress), null, ProgressInterval, ProgressInterval);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/sample-home.js");
            await _module.InvokeVoidAsync("init", _root);
        }
        catch (JSException)
        {
            // 脚本没就位（离线裁剪过的构建、脚本被拦截）时页面必须还能用：
            // 动画只是增强，卡片与交互本来就在 DOM 里。
            _module = null;
        }
    }

    private void AdvanceProgress()
    {
        if (_disposed)
        {
            return;
        }

        _progress = _progress >= 96 ? 12 : _progress + 9;
        StateHasChanged();
    }

    private void GoToComponents() => Navigation.NavigateTo(ShowcaseCatalog.RouteFor("install"));

    private Task ShowToastAsync()
    {
        DialogService.ShowToast("Toast 不打断当前操作，适合「已保存」这类结果。");
        return Task.CompletedTask;
    }

    private Task ShowAlertAsync() => DialogService.AlertAsync("Alert 是模态提示，需要用户显式关掉。");

    private Task ShowConfirmAsync() =>
        DialogService.ConfirmAsync("Confirm 把结果交回调用方：确认为 true，取消为 false。");

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _progressPulse?.Dispose();
        _progressPulse = null;

        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("dispose");
            await _module.DisposeAsync();
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }

        _module = null;
    }

    private sealed record ShowcaseGroup(string Title, IReadOnlyList<ShowcaseCard> Cards);

    private sealed record ShowcaseCard(string Id, string Preview, string Name, string Note)
    {
        public string Href => ShowcaseCatalog.RouteFor(Id);
    }
}
