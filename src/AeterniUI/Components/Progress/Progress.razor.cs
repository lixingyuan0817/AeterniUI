using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Progress;

public partial class Progress : AeterniComponent
{
    [Parameter]
    public double? Value { get; set; }
    [Parameter]
    public double Max { get; set; } = 100;
    [Parameter]
    public bool Indeterminate { get; set; }
    [Parameter]
    public Color Color { get; set; } = Color.Primary;
    [Parameter]
    public Size Size { get; set; } = Size.Default;
    [Parameter]
    public bool ShowValue { get; set; }
    [Parameter]
    public string? AriaLabel { get; set; }

    private double Percentage => Indeterminate || !Value.HasValue ? 0 : Math.Clamp(Value.Value / Max * 100, 0, 100);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-progress")
        .Add(SizeClass)
        .Add(ColorClass)
        .Add("is-indeterminate", Indeterminate);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "progressbar",
            ["aria-valuemin"] = "0",
            ["aria-valuemax"] = Max.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        if (!Indeterminate && Value.HasValue)
            attributes["aria-valuenow"] = Math.Clamp(Value.Value, 0, Max).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(AriaLabel)) attributes["aria-label"] = AriaLabel!;
        return attributes;
    }

    private string? SizeClass => ComponentClass.ForSize("aeterni-progress", Size);
    private string? ColorClass => ComponentClass.ForColor("aeterni-progress", Color);

    private void ValidateParameters()
    {
        if (Max <= 0 || double.IsNaN(Max) || double.IsInfinity(Max)) throw new ArgumentOutOfRangeException(nameof(Max));
        if (Value is { } value && (double.IsNaN(value) || double.IsInfinity(value))) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!Enum.IsDefined(Color)) throw new ArgumentOutOfRangeException(nameof(Color));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ValidateParameters();
    }
}
