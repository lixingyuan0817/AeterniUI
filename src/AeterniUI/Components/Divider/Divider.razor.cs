using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Divider;

/// <summary>
/// Horizontal or vertical separator with an optional label in the gap.
/// </summary>
public partial class Divider : AeterniComponent
{
    /// <summary>
    /// Line direction. <see cref="Orientation.Vertical" /> stretches to the height
    /// of the flex row that contains it and is mirrored by <c>aria-orientation</c>.
    /// </summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>
    /// Optional label (text or an <c>Icon</c>) placed between two line segments.
    /// When it is omitted the component renders a single continuous line.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown divider orientation.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-divider")
        .Add("aeterni-divider--vertical", Orientation == Orientation.Vertical)
        .Add("is-labeled", ChildContent is not null);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        // A separator needs its orientation as an explicit value: the role has no
        // vertical default, and Blazor would render a bool as a minimized attribute.
        return new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "separator",
            ["aria-orientation"] = Orientation == Orientation.Vertical ? "vertical" : "horizontal"
        };
    }
}
