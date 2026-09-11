using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Skeleton;

/// <summary>
/// Content placeholder. The bars are decorative (<c>aria-hidden</c>); the host
/// container is the one that announces the busy state.
/// </summary>
public partial class Skeleton : AeterniComponent
{
    /// <summary>Placeholder shape.</summary>
    [Parameter]
    public SkeletonVariant Variant { get; set; } = SkeletonVariant.Text;

    /// <summary>
    /// Number of stacked bars. Text blocks use it for multi-line placeholders; the
    /// last bar is shortened so the block does not end on a hard edge.
    /// </summary>
    [Parameter]
    public int Lines { get; set; } = 1;

    /// <summary>Bar width as a CSS length. Defaults to the full container width.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    /// Bar height as a CSS length. Defaults to the variant: one text line, the
    /// medium control height, or the large icon box for circles.
    /// </summary>
    [Parameter]
    public string? Height { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown skeleton variant.");
        }

        if (Lines < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Lines), Lines, "A skeleton needs at least one bar.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-skeleton")
        .Add($"aeterni-skeleton--{Variant.ToString().ToLowerInvariant()}");

    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-skeleton-width", Width, !string.IsNullOrWhiteSpace(Width))
        .Add("--aeterni-skeleton-height", Height, !string.IsNullOrWhiteSpace(Height));

    private ClassBuilder BarClass(int index) => ClassBuilder("aeterni-skeleton__bar")
        .Add("is-last", Lines > 1 && index == Lines - 1);
}
