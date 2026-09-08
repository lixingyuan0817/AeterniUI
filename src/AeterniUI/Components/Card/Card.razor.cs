using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Card;

public partial class Card : AeterniComponent
{
    [Parameter]
    public SurfaceVariant Variant { get; set; } = SurfaceVariant.Default;

    [Parameter]
    public SurfaceElevation Elevation { get; set; } = SurfaceElevation.None;

    [Parameter]
    public SurfacePadding Padding { get; set; } = SurfacePadding.Medium;

    [Parameter]
    public bool Bordered { get; set; } = true;

    [Parameter]
    public bool FullWidth { get; set; }

    [Parameter]
    public bool Interactive { get; set; }

    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Footer { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-card")
            .Add($"aeterni-card--{VariantClass}")
            .Add($"aeterni-card--elevation-{ElevationClass}")
            .Add($"aeterni-card--padding-{PaddingClass}")
            .Add("is-bordered", Bordered)
            .Add("is-full-width", FullWidth)
            .Add("is-interactive", IsInteractive)
            .Add("is-disabled", IsInteractive && Disabled);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (IsInteractive)
        {
            attributes["role"] = "button";

            if (!Disabled)
            {
                attributes["tabindex"] = "0";
            }
            else
            {
                attributes["aria-disabled"] = "true";
            }

            if (!string.IsNullOrWhiteSpace(AriaLabel))
            {
                attributes["aria-label"] = AriaLabel.Trim();
            }
        }

        return attributes;
    }

    private bool IsInteractive => Interactive || OnClick.HasDelegate;

    private async Task HandleClickAsync(MouseEventArgs _)
    {
        if (IsInteractive && !Disabled)
        {
            await OnClick.InvokeAsync();
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!IsInteractive || Disabled || (args.Key != "Enter" && args.Key != " "))
        {
            return;
        }

        await OnClick.InvokeAsync();
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
}
