using AeterniUI.Enums;
using AeterniUI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.IconButton;

/// <summary>A compact square button for an icon-only action.</summary>
public partial class IconButton : AeterniComponent
{
    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Ghost;

    [Parameter]
    public ButtonIntent Intent { get; set; } = ButtonIntent.Neutral;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private RenderFragment? EffectiveIcon => Icon ?? ChildContent;
}
