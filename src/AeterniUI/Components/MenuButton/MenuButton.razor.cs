using AeterniUI.Attributes;
using AeterniUI.Components.Button;
using AeterniUI.Enums;
using AeterniUI.Components;
using AeterniUI.Components.Menu;
using AeterniUI.Components.Popup;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.MenuButton;

/// <summary>A button that opens an anchored menu.</summary>
[JsModule("js/aeterni_menu_trigger.js", Name = "menu-trigger", Interactive = true)]
public partial class MenuButton : AeterniComponent
{
    private const string TriggerModule = "menu-trigger";

    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public IReadOnlyList<MenuGroup> Items { get; set; } = [];

    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Outline;

    [Parameter]
    public ButtonIntent Intent { get; set; } = ButtonIntent.Neutral;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private bool _internalOpen;
    private AeterniUI.Components.Button.Button? _triggerButton;
    private AeterniUI.Components.Menu.Menu? _menu;

    /// <summary>0 = nothing pending, 1 = focus the first node, 2 = focus the last.</summary>
    private int _pendingFocus;
    private bool _disposed;

    private bool EffectiveOpen => OpenChanged.HasDelegate ? Open : _internalOpen;

    [Parameter]
    public PopupPlacement Placement { get; set; } = PopupPlacement.BottomStart;

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public EventCallback<MenuItem> OnItemSelected { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        var trigger = _triggerButton is null ? default : _triggerButton.RootElement;
        await JsModuleManager.InvokeModuleVoidAsync(TriggerModule, "attach", InstanceId, trigger);

        if (_pendingFocus == 0)
        {
            return;
        }

        if (!EffectiveOpen || _menu is null)
        {
            _pendingFocus = 0;
            return;
        }

        var last = _pendingFocus == 2;
        _pendingFocus = 0;
        await _menu.FocusEdgeItemAsync(last);
    }

    /// <summary>
    /// ArrowDown/ArrowUp on the trigger opens the menu and places focus on its
    /// first/last reachable node. Arrow keys that arrive while the menu is already
    /// open only move focus; the module has already cancelled the page scroll.
    /// </summary>
    [JSInvokable]
    public async Task HandleTriggerArrowKeyAsync(bool arrowDown)
    {
        if (_disposed || Disabled || Loading || _menu is null)
        {
            return;
        }

        if (EffectiveOpen)
        {
            await _menu.FocusEdgeItemAsync(last: !arrowDown);
            return;
        }

        _pendingFocus = arrowDown ? 1 : 2;
        await SetOpenAsync(true);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Items);

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown menu button variant.");
        }

        if (!Enum.IsDefined(Intent))
        {
            throw new ArgumentOutOfRangeException(nameof(Intent), Intent, "Unknown menu button intent.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown menu button size.");
        }

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown menu button placement.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-menu-button")
        // REV-152: `is-open`/`is-disabled` had no rule anywhere; the state is carried
        // by `aria-expanded` and the native disabled attribute the trigger renders.
        .Add("is-full-width", FullWidth);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (Disabled || Loading)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private string MenuId => $"{ElementId}-menu";

    private IReadOnlyDictionary<string, object> TriggerAttributes => new Dictionary<string, object>
    {
        ["aria-haspopup"] = "menu",
        ["aria-expanded"] = EffectiveOpen ? "true" : "false",
        ["aria-controls"] = MenuId
    };

    private async Task ToggleAsync(MouseEventArgs _)
    {
        if (Disabled || Loading)
        {
            return;
        }

        await SetOpenAsync(!EffectiveOpen);
    }

    private async Task HandleItemSelectedAsync(MenuItem item)
    {
        await OnItemSelected.InvokeAsync(item);
        await SetOpenAsync(false);
    }

    private async Task HandleOpenChangedAsync(bool open)
    {
        await SetOpenAsync(open);
    }

    private async Task SetOpenAsync(bool open)
    {
        if (EffectiveOpen == open)
        {
            return;
        }

        if (OpenChanged.HasDelegate)
        {
            await OpenChanged.InvokeAsync(open);
        }
        else
        {
            _internalOpen = open;
            StateHasChanged();
        }
    }

    // The shared trigger module is released by the base class; this flag additionally
    // rejects a queued interop callback that arrives after the component is gone.
    protected override ValueTask OnComponentDisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
