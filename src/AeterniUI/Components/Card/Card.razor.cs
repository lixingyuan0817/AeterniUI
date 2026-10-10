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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown card variant.");
        }

        if (!Enum.IsDefined(Elevation))
        {
            throw new ArgumentOutOfRangeException(nameof(Elevation), Elevation, "Unknown card elevation.");
        }

        if (!Enum.IsDefined(Padding))
        {
            throw new ArgumentOutOfRangeException(nameof(Padding), Padding, "Unknown card padding.");
        }
    }

    // No Radius parameter, unlike Surface. The token layer states the reason:
    // Card carries a header, body and footer, so it reads as the larger container
    // and takes the fixed container radius, while Surface is a plain wrapper that
    // follows the control geometry and therefore exposes the scale. Adding the
    // option here would contradict that and duplicate six rule bodies that CSS
    // isolation cannot share.
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
    // The surface recipe is shared with the sibling container, so the
    // option-to-modifier mapping lives in ComponentClass rather than being written
    // out in both components and left to drift.
    private string? VariantClass => ComponentClass.ForSurfaceVariant(Variant);
    private string? ElevationClass => ComponentClass.ForSurfaceElevation(Elevation);
    private string? PaddingClass => ComponentClass.ForSurfacePadding(Padding);
}
