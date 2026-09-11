using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Drawer;

/// <summary>
/// A panel that slides in from a viewport edge. The modal form blocks the page
/// (backdrop, <c>aria-modal</c>, focus trap, background scroll lock, focus handed back
/// to the opener) while the non-modal form leaves the page interactive and never takes
/// focus. Both forms share the geometry, the Escape / outside-click request path and
/// the <c>Open</c> / <c>OpenChanged</c> contract.
/// </summary>
[JsModule("Components/Drawer/Drawer.razor.js", Name = "drawer", Interactive = true)]
public partial class Drawer : AeterniComponent
{
    private const string ModuleName = "drawer";

    /// <summary>
    /// Whether the panel is on screen. The parameter is controlled: closing (Escape,
    /// backdrop, outside pointer, close button) raises <see cref="OpenChanged" />
    /// instead of mutating it silently, so <c>@bind-Open</c> stays the single source
    /// of truth.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Edge the panel slides in from. Start/End follow the writing direction.</summary>
    [Parameter]
    public DrawerPlacement Placement { get; set; } = DrawerPlacement.End;

    /// <summary>
    /// Modal drawer (default): renders a backdrop, sets <c>aria-modal</c>, locks
    /// background scrolling, traps Tab inside the panel and returns focus to the
    /// opener on close. Set to <c>false</c> for a panel that leaves the page usable.
    /// </summary>
    [Parameter]
    public bool Modal { get; set; } = true;

    /// <summary>Whether Escape requests a close. Ignored when <see cref="AeterniComponent.Disabled" /> is set.</summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Whether a pointer press outside the panel requests a close. The modal form
    /// closes on its backdrop; the non-modal form closes on any press outside.
    /// </summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>Whether the header renders the built-in close button.</summary>
    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Header heading. It also names the panel for assistive technology.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Accessible name of the close button. Defaults to
    /// <see cref="Services.AeterniUITextOptions.DrawerCloseLabel" />.
    /// </summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    /// <summary>
    /// Accessible name used when no <see cref="Title" /> is set. Defaults to
    /// <see cref="Services.AeterniUITextOptions.DrawerLabel" />.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Panel body. It scrolls on its own when the content is taller than the panel.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional action bar pinned below the scrolling body.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>Referenced by the JS module for the focus trap and outside-pointer test.</summary>
    private ElementReference _panelElement;

    private bool ShowHeader => !string.IsNullOrWhiteSpace(Title) || ShowCloseButton;

    private string TitleId => $"{ElementId}-title";

    private string PanelRole => Modal ? "dialog" : "region";

    private string? PanelLabelledBy => string.IsNullOrWhiteSpace(Title) ? null : TitleId;

    private string? PanelLabel => string.IsNullOrWhiteSpace(Title)
        ? (string.IsNullOrWhiteSpace(AriaLabel) ? UiText.DrawerLabel : AriaLabel.Trim())
        : null;

    private string EffectiveCloseLabel => string.IsNullOrWhiteSpace(CloseLabel)
        ? UiText.DrawerCloseLabel
        : CloseLabel.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown drawer placement.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-drawer")
        .Add($"aeterni-drawer--{Placement.ToString().ToLowerInvariant()}")
        .Add("is-modal", Modal)
        .Add("is-open", Open);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);

        // The panel is removed from the layout when closed instead of being kept
        // around: nothing about a closed drawer should be focusable or clickable.
        if (!Open)
        {
            attributes["hidden"] = true;
        }

        if (Disabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
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
                _panelElement,
                Modal,
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
    /// Requests a close from the JS module (Escape, outside pointer). Kept public so the
    /// module can call it through the component's DotNet reference.
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
        // A disabled drawer keeps its state: every close path (button, Escape,
        // backdrop, outside pointer) funnels through here, so the lock is one guard.
        if (!Open || Disabled)
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
