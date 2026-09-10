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
            .Add($"aeterni-tag--{ColorClass}")
            .Add(SizeClass);
    }

    private string VariantClass => Variant switch
    {
        TagVariant.Soft => "soft",
        TagVariant.Outline => "outline",
        _ => "default"
    };

    private string? SizeClass => ComponentClass.ForSize("aeterni-tag", Size);

    private string? ColorClass => ComponentClass.ForColor("aeterni-tag", Color);
}
