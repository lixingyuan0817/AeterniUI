using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Tooltip;

/// <summary>
/// A non-blocking hover/focus-visible hint. The tooltip text is exposed as the
/// accessible description of the first focusable element inside
/// <see cref="ChildContent"/> (or of the trigger wrapper when the content is not
/// focusable), and the layer flips/shifts to stay inside the viewport.
/// </summary>
[JsModule("Components/Tooltip/Tooltip.razor.js", Name = "tooltip")]
public partial class Tooltip : AeterniComponent
{
    private const string ModuleName = "tooltip";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    public string? Text { get; set; }
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Preferred tooltip side. Defaults to <see cref="TooltipPlacement.Top"/>.
    /// </summary>
    [Parameter]
    public TooltipPlacement Placement { get; set; } = TooltipPlacement.Top;

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-tooltip")
        .Add($"aeterni-tooltip--{PlacementClass}");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(AriaLabel)) attributes["aria-label"] = AriaLabel!;
        return attributes;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Placement))
        {
            throw new ArgumentOutOfRangeException(nameof(Placement), Placement, "Unknown tooltip placement.");
        }
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || IsDisposed)
        {
            return;
        }

        await JsModuleManager.InvokeModuleVoidAsync(
            ModuleName,
            "attach",
            InstanceId,
            RootElement,
            HasTooltipContent ? ContentId : null,
            PlacementClass);
    }

    private bool HasTooltipContent => !Disabled && !string.IsNullOrWhiteSpace(Text);

    /// <summary>Id of the rendered tooltip node, referenced by aria-describedby.</summary>
    private string ContentId => $"{ElementId}-tooltip";

    private string PlacementClass => Placement switch
    {
        TooltipPlacement.Bottom => "bottom",
        TooltipPlacement.Start => "start",
        TooltipPlacement.End => "end",
        _ => "top"
    };
}
