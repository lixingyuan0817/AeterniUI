using AeterniUI.Enums;
using AeterniUI.Components;
using AeterniUI.Components.Menu;
using AeterniUI.Components.Popup;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.MenuButton;

/// <summary>A button that opens an anchored menu.</summary>
public partial class MenuButton : AeterniComponent
{
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

    protected override bool SupportsDisabled => true;

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-menu-button")
        .Add("is-open", EffectiveOpen)
        .Add("is-full-width", FullWidth);

    private string MenuId => $"{ElementId}-menu";

    private IReadOnlyDictionary<string, object> TriggerAttributes => new Dictionary<string, object>
    {
        ["aria-haspopup"] = "menu",
        ["aria-expanded"] = EffectiveOpen ? "true" : "false",
        ["aria-controls"] = MenuId
    };

    private IReadOnlyDictionary<string, object> MenuAttributes => new Dictionary<string, object>
    {
        ["id"] = MenuId
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
}
