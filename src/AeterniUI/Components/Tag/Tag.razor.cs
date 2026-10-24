using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Tag;

public partial class Tag : AeterniComponent
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? StartIcon { get; set; }

    [Parameter]
    public RenderFragment? EndIcon { get; set; }

    [Parameter]
    public Color Color { get; set; } = Color.Default;

    [Parameter]
    public TagVariant Variant { get; set; } = TagVariant.Default;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public bool Dismissible { get; set; }

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Accessible name of the dismiss button. Defaults to
    /// <see cref="AeterniUITextOptions.TagDismissLabel"/>.
    /// </summary>
    [Parameter]
    public string DismissLabel { get; set; } = string.Empty;

    private string EffectiveDismissLabel => string.IsNullOrWhiteSpace(DismissLabel)
        ? UiText.TagDismissLabel
        : DismissLabel;

    // A disabled tag must not dismiss, even though the close button also renders the
    // native disabled attribute; the guard keeps the contract true for keyboard or
    // synthetic activation paths that reach the handler anyway.
    private Task HandleDismissAsync(MouseEventArgs args) =>
        Disabled ? Task.CompletedTask : OnDismiss.InvokeAsync();

    // The tag root is a <span>, which has no native disabled state. Opting into the
    // base disabled contract gets us the aria-disabled announcement and a single
    // place to build the is-disabled class from; the native `disabled` attribute is
    // dropped again in BuildAttributes because it would be invalid on a span.
    protected override bool SupportsDisabled => true;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Color))
        {
            throw new ArgumentOutOfRangeException(nameof(Color), Color, "Unknown tag color.");
        }

        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant), Variant, "Unknown tag variant.");
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown tag size.");
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-tag")
            .Add($"aeterni-tag--{VariantClass}")
            // Color.Default emits no modifier; interpolating the mapping directly
            // produced a stray "aeterni-tag--" class for the default colour.
            .Add(ComponentClass.ForColor("aeterni-tag", Color))
            .Add(SizeClass)
            // A parked tag still has to read as parked: the `disabled` parameter was
            // previously ignored by the rendering path entirely.
            .Add("is-disabled", Disabled);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        // SupportsDisabled lets the base class emit the state contract, but a span
        // cannot carry the native attribute; only its ARIA counterpart is valid here.
        attributes.Remove("disabled");

        return attributes;
    }

    // The default recipe lives in the base rule, so Default emits no modifier; Soft
    // and Outline keep their own (REV-150).
    private string? VariantClass => Variant switch
    {
        TagVariant.Soft => "soft",
        TagVariant.Outline => "outline",
        _ => null
    };

    private string? SizeClass => ComponentClass.ForSize("aeterni-tag", Size);
}
