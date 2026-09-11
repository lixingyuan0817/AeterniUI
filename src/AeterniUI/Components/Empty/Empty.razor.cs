using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Empty;

/// <summary>
/// Empty-state placeholder: icon, title, description and an optional action area.
/// The component draws no surface of its own, so it can sit inside a
/// <c>Card</c>, <c>Surface</c>, table body or any other host container.
/// </summary>
public partial class Empty : AeterniComponent
{
    /// <summary>Size tier of the placeholder artwork and typography.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    /// <summary>
    /// Custom artwork. When it is omitted the built-in
    /// <see cref="Icons.AeterniIcons.EmptyBox" /> glyph is rendered at the tier size.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Heading. Falls back to <see cref="Services.AeterniUITextOptions.EmptyTitle" />.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Supporting line. The paragraph is omitted when it is blank.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Action area, typically one or two <c>Button</c> instances.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? UiText.EmptyTitle : Title.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown empty state size.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-empty")
        .Add(ComponentClass.ForSize("aeterni-empty", Size));
}
