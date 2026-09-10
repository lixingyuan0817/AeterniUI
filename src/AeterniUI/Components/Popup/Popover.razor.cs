using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Popup;

public partial class Popover : AeterniComponent
{
    [Parameter]
    public bool Open { get; set; }
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>
    /// Switches the ARIA role from <c>region</c> to <c>dialog</c> and lifts the
    /// layer to the modal z-index. Modal chrome (backdrop, focus trap and
    /// background scroll lock) is not implemented yet.
    /// </summary>
    [Parameter]
    public bool Modal { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-popover").Add("is-open", Open).Add("is-modal", Modal);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = Modal ? "dialog" : "region",
            ["hidden"] = !Open
        };
        if (!string.IsNullOrWhiteSpace(AriaLabel)) attributes["aria-label"] = AriaLabel!;
        return attributes;
    }
}
