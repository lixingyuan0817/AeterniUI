using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Toolbar;

/// <summary>A background-free action container with one roving tab stop.</summary>
[JsModule("Components/Toolbar/Toolbar.razor.js", Name = "toolbar", Interactive = true)]
public partial class Toolbar : AeterniComponent
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;
    [Parameter, EditorRequired] public string AriaLabel { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation));
        }
        if (string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new ArgumentException("A toolbar requires an accessible name.", nameof(AriaLabel));
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-toolbar")
        .Add("aeterni-toolbar--vertical", Orientation == Orientation.Vertical);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "toolbar",
            ["aria-label"] = AriaLabel.Trim(),
            ["aria-orientation"] = Orientation == Orientation.Vertical ? "vertical" : "horizontal",
            ["aria-disabled"] = Disabled ? "true" : "false",
            ["tabindex"] = "-1"
        };
        if (Disabled)
        {
            attributes["inert"] = true;
        }
        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("toolbar", "sync", InstanceId, RootElement);
}
