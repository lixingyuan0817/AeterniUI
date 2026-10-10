using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Surface;

public partial class Surface : AeterniComponent
{
    [Parameter]
    public SurfaceVariant Variant { get; set; } = SurfaceVariant.Default;

    [Parameter]
    public SurfaceElevation Elevation { get; set; } = SurfaceElevation.None;

    [Parameter]
    public SurfacePadding Padding { get; set; } = SurfacePadding.Medium;

    [Parameter]
    public SurfaceRadius Radius { get; set; } = SurfaceRadius.Default;

    [Parameter]
    public bool Bordered { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown surface variant.");
        }

        if (!Enum.IsDefined(Elevation))
        {
            throw new ArgumentOutOfRangeException(nameof(Elevation), Elevation, "Unknown surface elevation.");
        }

        if (!Enum.IsDefined(Padding))
        {
            throw new ArgumentOutOfRangeException(nameof(Padding), Padding, "Unknown surface padding.");
        }

        if (!Enum.IsDefined(Radius))
        {
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius, "Unknown surface radius.");
        }
    }

    // Card renders the same recipe with a fixed container radius and no option for
    // it (see the token layer's container-radius note); the difference is
    // deliberate, so this component keeps the only RadiusClass.
    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-surface")
            .Add(ComponentClass.For("aeterni-surface", VariantClass))
            .Add(ComponentClass.For("aeterni-surface", ElevationClass))
            .Add(ComponentClass.For("aeterni-surface", PaddingClass))
            .Add(ComponentClass.For("aeterni-surface", RadiusClass))
            .Add("is-bordered", Bordered)
            .Add("is-full-width", FullWidth);
    }

    // Each mapper returns null for the tier that the component's base rule
    // already renders, so no modifier class is emitted without a matching rule.
    // The surface recipe is shared with the sibling container, so the
    // option-to-modifier mapping lives in ComponentClass rather than being written
    // out in both components and left to drift.
    private string? VariantClass => ComponentClass.ForSurfaceVariant(Variant);
    private string? ElevationClass => ComponentClass.ForSurfaceElevation(Elevation);
    private string? PaddingClass => ComponentClass.ForSurfacePadding(Padding);

    private string? RadiusClass => ComponentClass.ForSurfaceRadius(Radius);
}
