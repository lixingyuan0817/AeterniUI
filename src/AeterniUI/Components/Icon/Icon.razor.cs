using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Icon;

public partial class Icon : AeterniComponent
{
    [Parameter, EditorRequired]
    public IconDefinition Definition { get; set; } = null!;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public Color Color { get; set; } = Color.Default;

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? Title { get; set; }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-icon")
            .Add($"aeterni-icon--{SizeClass}")
            .Add($"aeterni-icon--{ColorClass}");
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        ArgumentNullException.ThrowIfNull(Definition);

        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            ["viewBox"] = Definition.ViewBox,
            ["fill"] = "currentColor",
            ["xmlns"] = "http://www.w3.org/2000/svg",
            ["focusable"] = "false"
        };

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["role"] = "img";
            attributes["aria-label"] = AriaLabel.Trim();
        }
        else
        {
            attributes["aria-hidden"] = "true";
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            attributes["title"] = Title.Trim();
        }

        return attributes;
    }

    private string SizeClass => Size switch
    {
        Size.Small => "sm",
        Size.Medium => "md",
        Size.Large => "lg",
        _ => "default"
    };

    private string ColorClass => Color switch
    {
        Color.Primary => "primary",
        Color.Neutral => "neutral",
        Color.Success => "success",
        Color.Warning => "warning",
        Color.Danger => "danger",
        Color.Info => "info",
        _ => "default"
    };
}
