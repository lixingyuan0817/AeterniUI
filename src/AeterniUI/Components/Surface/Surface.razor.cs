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
            .Add($"aeterni-surface--{VariantClass}")
            .Add($"aeterni-surface--elevation-{ElevationClass}")
            .Add($"aeterni-surface--padding-{PaddingClass}")
            .Add($"aeterni-surface--radius-{RadiusClass}")
            .Add("is-bordered", Bordered)
            .Add("is-full-width", FullWidth);
    }

    private string VariantClass => Variant switch
    {
        SurfaceVariant.Subtle => "subtle",
        SurfaceVariant.Elevated => "elevated",
        SurfaceVariant.Glass => "glass",
        _ => "default"
    };

    private string ElevationClass => Elevation switch
    {
        SurfaceElevation.Small => "small",
        SurfaceElevation.Medium => "medium",
        SurfaceElevation.Large => "large",
        _ => "none"
    };

    private string PaddingClass => Padding switch
    {
        SurfacePadding.None => "none",
        SurfacePadding.Small => "small",
        SurfacePadding.Large => "large",
        SurfacePadding.ExtraLarge => "extra-large",
        _ => "medium"
    };

    private string RadiusClass => Radius switch
    {
        SurfaceRadius.None => "none",
        SurfaceRadius.Small => "small",
        SurfaceRadius.Medium => "medium",
        SurfaceRadius.Large => "large",
        SurfaceRadius.ExtraLarge => "extra-large",
        SurfaceRadius.Round => "round",
        _ => "default"
    };
}
