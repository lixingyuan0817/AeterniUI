using System.ComponentModel;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Avatar;

/// <summary>
/// Source-compatible aliases for the former avatar-specific size enum.
/// New code should use the shared <see cref="Size"/> values directly.
/// </summary>
[Obsolete("Use AeterniUI.Enums.Size directly. Avatar.Size now uses the shared size enum.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AvatarSize
{
    public const Size Small = Size.Small;
    public const Size Default = Size.Default;
    public const Size Large = Size.Large;
}

public partial class Avatar : AeterniComponent
{
    [Parameter] public string? Src { get; set; }
    [Parameter] public string? Alt { get; set; }
    [Parameter] public string? Name { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public Size Size { get; set; } = Size.Default;
    [Parameter] public Color Color { get; set; } = Color.Default;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-avatar")
        .Add(ComponentClass.ForSize("aeterni-avatar", Size))
        .Add(ComponentClass.ForColor("aeterni-avatar", Color));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Color)) throw new ArgumentOutOfRangeException(nameof(Color));
        if (string.IsNullOrWhiteSpace(Src) && string.IsNullOrWhiteSpace(Name) && ChildContent is null)
        {
            throw new ArgumentException("Avatar requires Src, Name, or ChildContent.");
        }

        if (string.IsNullOrWhiteSpace(Src) && ChildContent is not null && string.IsNullOrWhiteSpace(EffectiveLabel))
        {
            throw new ArgumentException("An avatar with custom content requires AriaLabel, Alt, or Name.");
        }
    }

    private string? EffectiveLabel => FirstNonEmpty(AriaLabel, Alt, Name);

    private string ImageAlt => EffectiveLabel ?? string.Empty;

    private string Initials
    {
        get
        {
            var words = Name!.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
