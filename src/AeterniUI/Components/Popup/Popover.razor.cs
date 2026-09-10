using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Popup;

/// <summary>
/// An anchored floating layer placed inside a <see cref="PopupHost"/>. The layer is
/// positioned against the host, flips to the opposite side when it does not fit in
/// the viewport and is shifted along the cross axis to stay inside it. When
/// <see cref="Modal" /> is set it also blocks the page: backdrop, focus trap,
/// background scroll lock, Escape and overlay-click close.
/// </summary>
[JsModule("Components/Popup/Popover.razor.js", Name = "popover", Interactive = true)]
public partial class Popover : AeterniComponent
{
    private const string ModuleName = "popover";

    /// <summary>
    /// Whether the layer is visible. The parameter is controlled: closing (Escape,
    /// overlay click) raises <see cref="OpenChanged" /> instead of mutating it
    /// silently, so <c>@bind-Open</c> stays the single source of truth.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Modal layer: renders a backdrop, sets <c>aria-modal</c>, traps Tab inside the
    /// layer, locks background scrolling and restores focus on close.
    /// </summary>
    [Parameter]
    public bool Modal { get; set; }

    [Parameter]
    public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;

    /// <summary>
    /// Whether Escape requests a close. Ignored when <see cref="Disabled" /> is set.
    /// </summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Whether clicking outside the layer closes it. A modal popover closes on its
    /// backdrop; a non-modal popover closes on any pointer press outside it.
    /// </summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    internal string PlacementClass => Placement switch
    {
        PopupPlacement.BottomEnd => "bottom-end",
        PopupPlacement.TopStart => "top-start",
        PopupPlacement.TopEnd => "top-end",
        _ => "bottom-start"
    };

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-popover")
        .Add($"aeterni-popover--{PlacementClass}")
        .Add("is-open", Open)
        .Add("is-modal", Modal);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = Modal ? "dialog" : "region",
            ["hidden"] = !Open
        };

        if (Modal)
        {
            attributes["aria-modal"] = "true";
            // The layer is only focusable so the trap has a fallback when it holds
            // no interactive content.
            attributes["tabindex"] = "-1";
        }

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel!.Trim();
        }

        return attributes;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown popover placement.");
        }
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        try
        {
            await JsModuleManager.InvokeModuleVoidAsync(
                ModuleName,
                "setOpen",
                InstanceId,
                Open,
                RootElement,
                Modal,
                PlacementClass,
                CloseOnEscape && !Disabled,
                CloseOnOutsideClick && !Disabled);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; nothing left to synchronise.
        }
        catch (TaskCanceledException)
        {
            // Prerendering or navigation cancelled the interop call.
        }
    }

    /// <summary>
    /// Requests a close from the JS module (Escape, outside pointer). Kept public so
    /// the module can call it through the component's DotNet reference.
    /// </summary>
    [JSInvokable]
    public Task RequestCloseAsync() => CloseAsync();

    internal async Task HandleBackdropClickAsync()
    {
        if (Modal && CloseOnOutsideClick && !Disabled)
        {
            await CloseAsync();
        }
    }

    private async Task CloseAsync()
    {
        if (!Open)
        {
            return;
        }

        Open = false;

        if (OpenChanged.HasDelegate)
        {
            await OpenChanged.InvokeAsync(false);
        }
    }
}
