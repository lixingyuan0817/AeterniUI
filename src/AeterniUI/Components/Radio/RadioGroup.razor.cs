using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Radio;

public partial class RadioGroup<TValue> : AeterniComponent
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [Parameter]
    public TValue? Value { get; set; }
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Size cascaded to the contained <see cref="Radio{TValue}"/> items.
    /// A Radio can still override it with its own <c>Size</c> parameter.
    /// </summary>
    [Parameter]
    public Size? Size { get; set; }

    [Parameter]
    public bool Required { get; set; }
    [Parameter]
    public bool Invalid { get; set; }
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Layout direction of the options. <see cref="Orientation.Vertical" />
    /// renders a single column and is mirrored by <c>aria-orientation</c>.
    /// </summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    // The fieldset adopts the FormField input id, so the field label's `for`
    // resolves to an existing element; the accessible name is attached with
    // aria-labelledby because a fieldset has no implicit label association.
    private string GroupId => Id ?? FormField?.InputId ?? InstanceId;
    private string EffectiveName => Name ?? GroupId;

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-radio-group")
        .Add($"aeterni-radio-group--{OrientationClass}");

    private string OrientationClass => Orientation == Orientation.Vertical ? "vertical" : "horizontal";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown radio group orientation.");
        }
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        attributes["id"] = GroupId;
        attributes["aria-orientation"] = OrientationClass;

        if (FormField?.LabelId is { } labelId)
        {
            attributes["aria-labelledby"] = labelId;
        }

        if (FormField?.DescribedBy is { } describedBy)
        {
            attributes["aria-describedby"] = describedBy;
        }

        return attributes;
    }

    private RadioGroupContext Context => new(
        value => EqualityComparer<TValue?>.Default.Equals(Value, (TValue?)value),
        SelectAsync,
        Disabled,
        Required,
        Invalid,
        EffectiveName,
        Size);

    private async Task SelectAsync(object? value)
    {
        if (!Disabled && ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync((TValue?)value);
    }
}
