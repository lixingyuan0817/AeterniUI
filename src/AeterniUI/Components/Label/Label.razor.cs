using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Label;

public partial class Label : AeterniComponent
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    public string? For { get; set; }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-label");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(For)) attributes["for"] = For.Trim();
        return attributes;
    }
}
