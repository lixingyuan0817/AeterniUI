using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.ButtonGroup;

public partial class ButtonGroup : AeterniComponent
{
    [CascadingParameter]
    private ButtonGroupContext? ParentContext { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public bool Connected { get; set; } = true;

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-button-group")
            .Add($"aeterni-button-group--{OrientationClass}")
            .Add("is-connected", Connected)
            .Add("is-full-width", FullWidth)
            .Add("is-disabled", IsEffectivelyDisabled);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "group",
            ["data-orientation"] = OrientationClass
        };

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel.Trim();
        }

        if (IsEffectivelyDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private bool IsEffectivelyDisabled => Disabled || ParentContext?.Disabled == true;

    private ButtonGroupContext CurrentContext => new(IsEffectivelyDisabled);

    private string OrientationClass => Orientation == Orientation.Vertical
        ? "vertical"
        : "horizontal";
}
