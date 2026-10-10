using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Toolbar;

/// <summary>A background-free action container with one roving tab stop.</summary>
[JsModule("Components/Toolbar/Toolbar.razor.js", Name = "toolbar", Interactive = true)]
public partial class Toolbar : AeterniComponent
{
    private ElementReference _renderedRootElement;

    /// <summary>
    /// The rendered root node, bound instead of <see cref="AeterniComponent.RootElement" />.
    /// A host-supplied <c>Element</c> parameter replaces <c>RootElement</c> with an
    /// element the component does not own, which made the module's ownership check
    /// fail and silently disabled the roving keyboard model (REV-147).
    /// </summary>
    private ElementReference RenderedRootElement
    {
        get => _renderedRootElement;
        set
        {
            _renderedRootElement = value;
            RootElement = value;
        }
    }

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;
    [Parameter, EditorRequired] public string AriaLabel { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown toolbar orientation.");
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
            ["tabindex"] = "-1"
        };
        if (Disabled)
        {
            // Emitted only while disabled, matching the base class and the rest of
            // the library.
            attributes["aria-disabled"] = "true";
            attributes["inert"] = true;
        }
        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("toolbar", "sync", InstanceId, RenderedRootElement);
}
