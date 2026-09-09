using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Tooltip;

public partial class Tooltip : AeterniComponent
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Text { get; set; }
    [Parameter] public string? AriaLabel { get; set; }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-tooltip");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(AriaLabel)) attributes["aria-label"] = AriaLabel!;
        return attributes;
    }
}
