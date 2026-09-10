using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Tabs;

/// <summary>
/// One tab: a <c>tab</c> button (rendered by <see cref="Tabs" />, which owns the
/// strip) and the <c>tabpanel</c> the component renders itself. Identify the tab
/// with <see cref="Value" />; ids for the ARIA wiring are derived from the
/// registration order, so they are always unique and always paired.
/// </summary>
public partial class Tab : AeterniComponent
{
    [CascadingParameter]
    private Tabs? Owner { get; set; }

    /// <summary>
    /// Identifier of this tab inside its <see cref="Tabs" />. Must be unique and
    /// non-empty; it is what <c>Tabs.Value</c> compares against.
    /// </summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Content of the <c>tab</c> button in the strip.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>Content of the <c>tabpanel</c>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool Selected => Owner?.IsRenderedSelected(this) == true;

    protected override void OnComponentInitialized()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new InvalidOperationException("Tab requires a non-empty Value.");
        }

        Owner?.Register(this);
    }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-tabs__panel");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            // The panel id wins over the base element id: the strip points
            // aria-controls at it, so the pairing must not depend on the consumer.
            ["id"] = Owner?.TabPanelId(this) ?? ElementId,
            ["role"] = "tabpanel",
            ["aria-labelledby"] = Owner?.TabButtonId(this) ?? ElementId,
            // Panels without focusable content still need a Tab stop; the ARIA tabs
            // pattern covers both cases with a focusable panel.
            ["tabindex"] = "0",
            ["hidden"] = !Selected
        };

        if (Selected)
        {
            attributes.Remove("hidden");
        }

        return attributes;
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        Owner?.Unregister(this);
        return ValueTask.CompletedTask;
    }
}
