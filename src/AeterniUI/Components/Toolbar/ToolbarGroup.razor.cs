using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Toolbar;

/// <summary>A named action group that does not introduce another tab stop.</summary>
public partial class ToolbarGroup : AeterniComponent
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter, EditorRequired] public string AriaLabel { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new ArgumentException("A toolbar group requires an accessible name.", nameof(AriaLabel));
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-toolbar-group");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "group",
            ["aria-label"] = AriaLabel.Trim()
        };
        if (Disabled)
        {
            // Emitted only while disabled, matching the base class and the rest of
            // the library: an explicit "false" would be a second convention.
            attributes["aria-disabled"] = "true";
            attributes["inert"] = true;
        }
        return attributes;
    }
}
