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
    private string? VariantClass => Variant switch
    {
        SurfaceVariant.Subtle => "subtle",
        SurfaceVariant.Elevated => "elevated",
        SurfaceVariant.Glass => "glass",
        _ => null
    };

    private string? ElevationClass => Elevation switch
    {
        SurfaceElevation.Small => "elevation-small",
        SurfaceElevation.Medium => "elevation-medium",
        SurfaceElevation.Large => "elevation-large",
        _ => null
    };

    private string? PaddingClass => Padding switch
    {
        SurfacePadding.None => "padding-none",
        SurfacePadding.Small => "padding-small",
        SurfacePadding.Large => "padding-large",
        SurfacePadding.ExtraLarge => "padding-extra-large",
        _ => "padding-medium"
    };

    private string? RadiusClass => Radius switch
    {
        SurfaceRadius.None => "radius-none",
        SurfaceRadius.Small => "radius-small",
        SurfaceRadius.Medium => "radius-medium",
        SurfaceRadius.Large => "radius-large",
        SurfaceRadius.ExtraLarge => "radius-extra-large",
        SurfaceRadius.Round => "radius-round",
        _ => null
    };
}
