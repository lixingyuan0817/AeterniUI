using System.Globalization;
using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.FlashCard;

/// <summary>
/// An image flash card: a front face with media and an optional caption, plus an
/// optional back face that the user reveals by flipping the card. The pointer
/// following 3D tilt is the only browser behaviour; the flip is component state,
/// so the rendered sides and their ARIA stay in sync with <see cref="IsFlipped"/>.
/// </summary>
[JsModule("Components/FlashCard/FlashCard.razor.js", Name = "flash-card")]
public partial class FlashCard : AeterniComponent
{
    private const string ModuleName = "flash-card";

    private bool _internalFlipped;
    private bool _stateInitialized;

    /// <summary>Image shown on the front face.</summary>
    [Parameter]
    public string? ImageSrc { get; set; }

    /// <summary>
    /// Alternative text for <see cref="ImageSrc" />. An empty value marks the image
    /// as decorative; supply it whenever the image carries the card's meaning.
    /// </summary>
    [Parameter]
    public string? ImageAlt { get; set; }

    /// <summary>Width to height ratio of the card face.</summary>
    [Parameter]
    public double AspectRatio { get; set; } = 1;

    /// <summary>Front face heading.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Front face supporting text.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Back face content. Supplying it makes the card flippable.</summary>
    [Parameter]
    public RenderFragment? Back { get; set; }

    /// <summary>Whether the back face is currently shown.</summary>
    [Parameter]
    public bool IsFlipped { get; set; }

    /// <summary>Raised before the card is flipped, with the requested state.</summary>
    [Parameter]
    public EventCallback<bool> IsFlippedChanged { get; set; }

    /// <summary>
    /// How the back face is revealed. <see cref="FlashCardFlipTrigger.Click" /> is
    /// the default keyboard reachable control; <see cref="FlashCardFlipTrigger.Hover" />
    /// is a pointer-only preview that never changes <see cref="IsFlipped" />.
    /// </summary>
    [Parameter]
    public FlashCardFlipTrigger FlipTrigger { get; set; } = FlashCardFlipTrigger.Click;

    /// <summary>Whether the pointer follows the tilt. Reduced motion always disables it.</summary>
    [Parameter]
    public bool Tilt { get; set; } = true;

    /// <summary>Maximum rotation in degrees reached at the card edges.</summary>
    [Parameter]
    public double MaxTiltAngle { get; set; } = 16;

    /// <summary>Scale applied while the pointer is over the card.</summary>
    [Parameter]
    public double TiltScale { get; set; } = 1.02;

    /// <summary>CSS perspective distance in pixels.</summary>
    [Parameter]
    public double Perspective { get; set; } = 800;

    /// <summary>
    /// Surface finish of the media area. <see cref="FlashCardSheen.Holo" /> is the
    /// rainbow diffraction finish of a holographic trading card,
    /// <see cref="FlashCardSheen.Shine" /> the metallic white sweep, and
    /// <see cref="FlashCardSheen.None" /> renders no overlay.
    /// </summary>
    [Parameter]
    public FlashCardSheen Sheen { get; set; } = FlashCardSheen.Holo;

    /// <summary>Strength of the sheen overlay, between 0 and 1.</summary>
    [Parameter]
    public double SheenIntensity { get; set; } = 0.6;

    /// <summary>Accessible name of the flip button; falls back to <see cref="Title" />.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!_stateInitialized && !IsControlled)
        {
            _internalFlipped = IsFlipped;
            _stateInitialized = true;
        }

        if (!double.IsFinite(AspectRatio) || AspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AspectRatio), AspectRatio, "AspectRatio must be a positive finite number.");
        }

        if (!Enum.IsDefined(FlipTrigger))
        {
            throw new ArgumentOutOfRangeException(nameof(FlipTrigger), FlipTrigger, "Unknown flash card flip trigger.");
        }

        if (!double.IsFinite(Perspective) || Perspective <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Perspective), Perspective, "Perspective must be a positive finite number.");
        }

        if (!double.IsFinite(MaxTiltAngle) || MaxTiltAngle < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxTiltAngle), MaxTiltAngle, "MaxTiltAngle must be a non-negative finite number.");
        }

        if (!double.IsFinite(TiltScale) || TiltScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TiltScale), TiltScale, "TiltScale must be a positive finite number.");
        }

        if (!Enum.IsDefined(Sheen))
        {
            throw new ArgumentOutOfRangeException(nameof(Sheen), Sheen, "Unknown flash card sheen finish.");
        }

        if (!double.IsFinite(SheenIntensity) || SheenIntensity < 0 || SheenIntensity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(SheenIntensity), SheenIntensity, "SheenIntensity must be between 0 and 1.");
        }

        if (!HasImage && !HasTitle && !HasDescription && Back is null)
        {
            throw new ArgumentException("FlashCard requires ImageSrc, Title, Description, or Back content.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-flash-card")
        .Add(ComponentClass.For("aeterni-flash-card", SheenClass))
        .Add("is-flippable", CanFlip)
        .Add("is-flip-hover", HoverPreview)
        .Add("is-flipped", CanFlip && CurrentFlipped)
        .Add("is-disabled", Disabled);

    // The pointer rotation, the hover scale and the sheen position are consumed by
    // the isolated stylesheet; the angle itself is computed in JS from MaxTiltAngle.
    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-flash-card-aspect", AspectRatio.ToString(CultureInfo.InvariantCulture))
        .Add("--aeterni-flash-card-scale", TiltScale.ToString(CultureInfo.InvariantCulture))
        .Add("--aeterni-flash-card-sheen-opacity", SheenIntensity.ToString(CultureInfo.InvariantCulture))
        .Add("--aeterni-flash-card-perspective", $"{Perspective.ToString(CultureInfo.InvariantCulture)}px");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);

        if (!UsesClickFlip)
        {
            return attributes;
        }

        attributes["role"] = "button";
        attributes["aria-pressed"] = CurrentFlipped ? "true" : "false";

        if (Disabled)
        {
            // The root is a div, so it cannot carry the native disabled attribute:
            // the state is announced and the card leaves the tab sequence instead.
            attributes["aria-disabled"] = "true";
        }
        else
        {
            attributes["tabindex"] = "0";
        }

        var label = FirstNonBlank(AriaLabel, Title);
        if (label is not null)
        {
            attributes["aria-label"] = label;
        }

        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync(
            ModuleName,
            "attach",
            InstanceId,
            RootElement,
            Tilt && !Disabled,
            MaxTiltAngle,
            HasSheen);

    private bool IsControlled => IsFlippedChanged.HasDelegate;

    private bool CurrentFlipped => IsControlled ? IsFlipped : _internalFlipped;

    private bool CanFlip => Back is not null;

    /// <summary>
    /// Click flip is the keyboard reachable control state: the card owns the
    /// pressed state and hides the face that is not showing.
    /// </summary>
    private bool UsesClickFlip => CanFlip && FlipTrigger == FlashCardFlipTrigger.Click;

    /// <summary>
    /// Hover flip is a pointer-only preview driven by the isolated stylesheet. It
    /// never changes state, so neither face is hidden from assistive technology and
    /// the card exposes no button semantics.
    /// </summary>
    private bool HoverPreview => CanFlip && FlipTrigger == FlashCardFlipTrigger.Hover;

    private bool IsBackVisible => UsesClickFlip && CurrentFlipped;

    private bool IsBackHidden => UsesClickFlip && !CurrentFlipped;

    private bool HasImage => !string.IsNullOrWhiteSpace(ImageSrc);

    /// <summary>
    /// The sheen only exists over media: a card without an image has no surface to
    /// reflect, and covering the caption would dim the text it carries.
    /// </summary>
    private bool HasSheen => HasImage && Sheen != FlashCardSheen.None;

    // Each mapper returns null for the tier that the component's base rule already
    // renders, so no modifier class is emitted without a matching rule.
    private string? SheenClass => Sheen switch
    {
        FlashCardSheen.Shine => "sheen-shine",
        FlashCardSheen.Holo => "sheen-holo",
        _ => null
    };

    private bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    private bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    private bool HasCaption => HasTitle || HasDescription;

    private string ImageAltValue => ImageAlt?.Trim() ?? string.Empty;

    /// <summary>
    /// Click handler bound only for flippable cards. A default
    /// <see cref="EventCallback{T}" /> renders no attribute at all, so a static
    /// card registers no DOM listener.
    /// </summary>
    private EventCallback<MouseEventArgs> ClickHandler => UsesClickFlip
        ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync)
        : default;

    /// <summary>Keyboard handler bound only for click flipped cards.</summary>
    private EventCallback<KeyboardEventArgs> KeyDownHandler => UsesClickFlip
        ? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync)
        : default;

    private async Task HandleClickAsync(MouseEventArgs _) => await ToggleAsync();

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is not ("Enter" or " "))
        {
            return;
        }

        await ToggleAsync();
    }

    private async Task ToggleAsync()
    {
        if (!UsesClickFlip || Disabled)
        {
            return;
        }

        var next = !CurrentFlipped;
        if (!IsControlled)
        {
            _internalFlipped = next;
        }

        if (IsFlippedChanged.HasDelegate)
        {
            await IsFlippedChanged.InvokeAsync(next);
        }
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
