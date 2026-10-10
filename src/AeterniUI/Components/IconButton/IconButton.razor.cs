using AeterniUI.Enums;
using AeterniUI.Components;
using AeterniUI.Components.ButtonGroup;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.IconButton;

/// <summary>A compact square button for an icon-only action.</summary>
public partial class IconButton : AeterniComponent
{
    [CascadingParameter]
    private ButtonGroupContext? ParentContext { get; set; }

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

    private bool IsEffectivelyDisabled => Disabled || (ParentContext?.Disabled ?? false);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown icon button variant.");
        }

        if (!Enum.IsDefined(Intent))
        {
            throw new ArgumentOutOfRangeException(nameof(Intent), Intent, "Unknown icon button intent.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown icon button size.");
        }

        // An undefined value would otherwise be rendered verbatim as `type="99"`,
        // and HTML resolves an invalid type to `submit`.
        if (!Enum.IsDefined(Type))
        {
            throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown button type.");
        }

        // An icon-only button has no text of its own, so the accessible name is the
        // only thing that says what it does. Validating here also keeps the
        // AriaLabel.Trim() in the markup from dereferencing null, matching the
        // ToggleGroup / Toolbar / ToolbarGroup / FlashCardGroup precedent.
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new ArgumentException("An icon button requires an accessible name.", nameof(AriaLabel));
        }
    }

    // REV-157: the render logic lives here with the parameters, like Button,
    // SplitButton and ToggleGroup, instead of in the markup's @code block.
    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (IsEffectivelyDisabled || Loading)
        {
            return;
        }

        await OnClick.InvokeAsync(args);
    }

    protected override bool SupportsDisabled => true;

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-icon-button")
        .Add($"aeterni-icon-button--{VariantClass}")
        .Add(IntentClass)
        .Add(SizeClass)
        .Add("is-loading", Loading);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = ComponentClass.ForButtonType(Type),
            ["aria-label"] = AriaLabel.Trim()
        };

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

    private string VariantClass => Variant switch
    {
        ButtonVariant.Outline => "outline",
        ButtonVariant.Soft => "soft",
        ButtonVariant.Ghost => "ghost",
        ButtonVariant.Link => "link",
        _ => "solid"
    };

    private string IntentClass => Intent switch
    {
        ButtonIntent.Neutral => "aeterni-icon-button--neutral",
        ButtonIntent.Warning => "aeterni-icon-button--warning",
        ButtonIntent.Danger => "aeterni-icon-button--danger",
        _ => "aeterni-icon-button--default"
    };

    private string? SizeClass => ComponentClass.ForSize("aeterni-icon-button", Size);
}
