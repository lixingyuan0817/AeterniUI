using System.Globalization;
using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Splitter;

/// <summary>
/// 两个面板之间的可拖动分栏布局。第一栏使用可配置的像素宽度，第二栏占满剩余空间。
/// </summary>
[JsModule("Components/Splitter/Splitter.razor.js", Name = "splitter", Interactive = true)]
public partial class Splitter : AeterniComponent
{
    /// <summary>
    /// 分栏方向。<see cref="Orientation.Horizontal" /> 左右排列（分隔条为垂直方向）；
    /// <see cref="Orientation.Vertical" /> 上下排列。
    /// </summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>第一栏的初始像素宽度。</summary>
    [Parameter]
    public int FirstSize { get; set; } = 280;

    /// <summary>第一栏允许的最小像素宽度。</summary>
    [Parameter]
    public int MinFirstSize { get; set; } = 200;

    /// <summary>第一栏允许的最大像素宽度。</summary>
    [Parameter]
    public int MaxFirstSize { get; set; } = 600;

    /// <summary>
    /// 指针拖动、键盘调整或双击复位结束后触发。拖动过程中的实时宽度由 JS 直接写入
    /// CSS 变量，因此该回调不会随每个指针移动触发。
    /// </summary>
    [Parameter]
    public EventCallback<int> FirstSizeChanged { get; set; }

    /// <summary>
    /// 可选的 localStorage 键。提供时恢复该键上次保存的宽度，并在每次提交后保存。
    /// </summary>
    [Parameter]
    public string? PersistKey { get; set; }

    /// <summary>分隔条的可访问名称；未提供时使用组件库文案表。</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>第一栏（尺寸受控）的内容。</summary>
    [Parameter, EditorRequired]
    public RenderFragment? FirstPane { get; set; }

    /// <summary>第二栏的内容，占满剩余空间。</summary>
    [Parameter, EditorRequired]
    public RenderFragment? SecondPane { get; set; }

    private ElementReference _renderedRootElement;
    private ElementReference _renderedHandle;
    private int _size;
    private bool _hasSize;
    private int _lastFirstSize;
    private bool _needsSync;
    private bool _attached;

    /// <summary>
    /// The element this component rendered. REV-147: feeding the rendered node back to
    /// <see cref="AeterniComponent.RootElement" /> keeps <c>Element</c> / <c>ElementChanged</c>
    /// alive; binding <c>@ref</c> straight to a private field leaves both parameters dead.
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

    private string SeparatorOrientation =>
        Orientation == Orientation.Horizontal ? "vertical" : "horizontal";

    private string EffectiveAriaLabel =>
        string.IsNullOrWhiteSpace(AriaLabel) ? UiText.SplitterLabel : AriaLabel.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown splitter orientation.");
        }

        if (MinFirstSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinFirstSize), MinFirstSize, "The minimum pane size cannot be negative.");
        }

        if (MaxFirstSize <= MinFirstSize)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFirstSize), MaxFirstSize, "The maximum pane size must be greater than the minimum.");
        }

        if (FirstSize < MinFirstSize || FirstSize > MaxFirstSize)
        {
            throw new ArgumentOutOfRangeException(nameof(FirstSize), FirstSize, "The first pane size must be within the minimum and maximum.");
        }

        // Keep the last dragged width when the host re-renders with the same
        // FirstSize value. A changed parameter is a deliberate external reset.
        // The flag, not a 0 sentinel: 0 is a legal size when MinFirstSize is 0.
        if (!_hasSize || FirstSize != _lastFirstSize)
        {
            _size = FirstSize;
            _hasSize = true;
        }

        _lastFirstSize = FirstSize;
        _needsSync = true;
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-splitter")
        .Add("aeterni-splitter--vertical", Orientation == Orientation.Vertical);

    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-splitter-first-size", $"{_size.ToString(CultureInfo.InvariantCulture)}px");

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (_attached)
        {
            if (_needsSync)
            {
                await JsModuleManager.InvokeModuleVoidAsync("splitter", "sync", InstanceId, RenderedRootElement, _renderedHandle, BuildOptions());
                _needsSync = false;
            }

            return;
        }

        if (string.IsNullOrEmpty(RenderedRootElement.Id))
        {
            return;
        }

        await JsModuleManager.InvokeModuleVoidAsync("splitter", "attach", InstanceId, RenderedRootElement, _renderedHandle, BuildOptions());
        _attached = true;
        _needsSync = false;
    }

    /// <summary>
    /// 浏览器在一次已提交的拖动、键盘或复位手势后调用。JS 已经绘制了相同宽度，
    /// 这里只同步 Blazor 状态并触发公开回调。
    /// </summary>
    [JSInvokable]
    public Task OnSizeChangedAsync(int size) => ApplyCommittedSizeAsync(size);

    /// <summary>
    /// JS 恢复持久化宽度时调用；本次挂载中恢复值优先于初始 FirstSize 参数。
    /// </summary>
    [JSInvokable]
    public Task OnSizeRestoredAsync(int size) => ApplyCommittedSizeAsync(size);

    private async Task ApplyCommittedSizeAsync(int size)
    {
        if (IsDisposed)
        {
            return;
        }

        _size = Math.Clamp(size, MinFirstSize, MaxFirstSize);
        _hasSize = true;
        if (FirstSizeChanged.HasDelegate)
        {
            await FirstSizeChanged.InvokeAsync(_size);
        }

        await InvokeAsync(StateHasChanged);
    }

    private object BuildOptions() => new
    {
        orientation = Orientation == Orientation.Horizontal ? "horizontal" : "vertical",
        min = MinFirstSize,
        max = MaxFirstSize,
        firstSize = _size,
        persistKey = string.IsNullOrWhiteSpace(PersistKey) ? null : PersistKey.Trim(),
        disabled = Disabled
    };
}
