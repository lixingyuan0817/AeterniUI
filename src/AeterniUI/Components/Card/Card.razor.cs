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
            .Add(ComponentClass.For("aeterni-card", VariantClass))
            .Add(ComponentClass.For("aeterni-card", ElevationClass))
            .Add(ComponentClass.For("aeterni-card", PaddingClass))
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

    /// <summary>
    /// Click handler that is only bound for an interactive card. A default
    /// <see cref="EventCallback{T}" /> renders no attribute at all, so a static card
    /// registers no DOM listener.
    /// </summary>
    private EventCallback<MouseEventArgs> ClickHandler => IsInteractive
        ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync)
        : default;

    /// <summary>
    /// Keyboard handler that is only bound for an interactive card.
    /// </summary>
    private EventCallback<KeyboardEventArgs> KeyDownHandler => IsInteractive
        ? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync)
        : default;

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
}
