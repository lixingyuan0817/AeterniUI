using AeterniUI.Components.Menu;
using AeterniUI.Components.Popup;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.IconButtonMenu;

/// <summary>
/// An icon-only action button that opens an anchored menu. It composes the
/// existing <c>IconButton</c>, <c>PopupHost</c>, <c>Popover</c> and <c>Menu</c>;
/// the trigger is the only interactive element and the menu reuses the existing
/// keyboard, Escape, outside-click and focus-restore behaviour.
/// </summary>
public partial class IconButtonMenu : AeterniComponent
{
    /// <summary>The icon glyph of the trigger button.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? Icon { get; set; }

    /// <summary>The grouped menu content. Flat mode renders it as one list.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<MenuGroup> Items { get; set; } = [];

    /// <summary>
    /// Accessible name of the trigger button. Unlike <c>MenuButton</c> the trigger
    /// has no visible label, so this is the only thing that names the action.
    /// </summary>
    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Ghost;

    [Parameter]
    public ButtonIntent Intent { get; set; } = ButtonIntent.Neutral;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    /// <summary>
    /// Trigger outline: <see cref="IconButtonShape.Circle" /> (default, matching
    /// <c>IconButton</c>) or <see cref="IconButtonShape.Square" /> for square chrome.
    /// Forwarded to the inner <c>IconButton</c>.
    /// </summary>
    [Parameter]
    public IconButtonShape Shape { get; set; } = IconButtonShape.Circle;

    [Parameter]
    public PopupPlacement Placement { get; set; } = PopupPlacement.BottomEnd;

    /// <summary>
    /// Passed through to <c>Menu.Flat</c>: an icon menu with a single "more
    /// actions" role usually has no group headers.
    /// </summary>
    [Parameter]
    public bool Flat { get; set; }

    /// <summary>
    /// Optional controlled open state. Like <c>MenuButton</c>, the component owns
    /// the state until <see cref="OpenChanged" /> is bound.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<MenuItem> OnItemSelected { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    private bool _internalOpen;

    private bool EffectiveOpen => OpenChanged.HasDelegate ? Open : _internalOpen;

    private string MenuId => $"{ElementId}-menu";

    /// <summary>
    /// ARIA on the real trigger button. <c>aria-haspopup</c> announces the popup
    /// kind, <c>aria-expanded</c> tracks the effective open state and
    /// <c>aria-controls</c> points at the menu root id that <c>Menu</c> renders.
    /// </summary>
    private IReadOnlyDictionary<string, object> TriggerAttributes => new Dictionary<string, object>
    {
        ["aria-haspopup"] = "menu",
        ["aria-expanded"] = EffectiveOpen ? "true" : "false",
        ["aria-controls"] = MenuId
    };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Items);
        // The trigger has no text content of its own, so an icon is the only
        // visible affordance; an empty one would render a nameless square.
        ArgumentNullException.ThrowIfNull(Icon);

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown icon button menu variant.");
        }

        if (!Enum.IsDefined(Intent))
        {
            throw new ArgumentOutOfRangeException(nameof(Intent), Intent, "Unknown icon button menu intent.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown icon button menu size.");
        }

        if (!Enum.IsDefined(Shape))
        {
            throw new ArgumentOutOfRangeException(nameof(Shape), Shape, "Unknown icon button menu shape.");
        }

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown icon button menu placement.");
        }

        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new ArgumentException("An icon button menu requires an accessible name.", nameof(AriaLabel));
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-icon-button-menu");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (Disabled || Loading)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

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
}
