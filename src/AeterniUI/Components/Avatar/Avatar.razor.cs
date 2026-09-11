using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Avatar;

public enum AvatarSize { Small, Default, Large }

public partial class Avatar : AeterniComponent
{
    [Parameter] public string? Src { get; set; }
    [Parameter] public string? Alt { get; set; }
    [Parameter] public string? Name { get; set; }
    [Parameter] public AvatarSize Size { get; set; } = AvatarSize.Default;
    [Parameter] public Color Color { get; set; } = Color.Primary;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-avatar")
        .Add($"aeterni-avatar--{Size.ToString().ToLowerInvariant()}")
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
    }

    private string Initials
    {
        get
        {
            var words = (Name ?? "?").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
        }
    }
}
