using AeterniUI.Enums;
using AeterniUI.Components.ButtonGroup;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Button;

public partial class Button : AeterniComponent
{
    [CascadingParameter]
    private ButtonGroupContext? ButtonGroupContext { get; set; }

    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Default;

    [Parameter]
    public Color Color { get; set; } = Color.Default;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>
    /// Legacy shorthand for <see cref="StartIcon"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public RenderFragment? StartIcon { get; set; }

    [Parameter]
    public RenderFragment? EndIcon { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    protected override bool SupportsDisabled => true;

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-button")
            .Add(SizeClass)
            .Add($"aeterni-button--{VariantClass}")
            .Add(ColorClass)
            .Add("is-icon-only", IsIconOnly)
            .Add("is-loading", Loading)
            .Add("is-disabled", IsEffectivelyDisabled)
            .Add("is-full-width", FullWidth);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        attributes["type"] = Type.ToString().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel;
        }

        if (IsEffectivelyDisabled || Loading)
        {
            attributes["disabled"] = true;
            attributes["aria-disabled"] = "true";
        }

        if (Loading)
        {
            attributes["aria-busy"] = "true";
        }

        return attributes;
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (IsEffectivelyDisabled || Loading)
        {
            return;
        }

        await OnClick.InvokeAsync(args);
    }

    private bool HasTextContent => ChildContent is not null;

    private bool IsEffectivelyDisabled => Disabled || ButtonGroupContext?.Disabled == true;

    private RenderFragment? EffectiveStartIcon => StartIcon ?? Icon;

    private bool IsIconOnly =>
        EffectiveStartIcon is not null && EndIcon is null && !HasTextContent;

    private string? SizeClass => ComponentClass.ForSize("aeterni-button", Size);

    private string VariantClass => Variant switch
    {
        ButtonVariant.Outline => "outline",
        ButtonVariant.Ghost => "ghost",
        ButtonVariant.Text => "text",
        ButtonVariant.Link => "link",
        _ => "default"
    };

    private string? ColorClass => ComponentClass.ForColor("aeterni-button", Color);
}
