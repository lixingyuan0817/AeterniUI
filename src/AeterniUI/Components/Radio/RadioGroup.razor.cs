using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Radio;

public partial class RadioGroup<TValue> : AeterniComponent
{
    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Name { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }
    [Parameter] public string? AriaLabel { get; set; }

    private string GroupId => Id ?? InstanceId;
    private string EffectiveName => Name ?? GroupId;

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-radio-group");

    private RadioGroupContext Context => new(
        value => EqualityComparer<TValue?>.Default.Equals(Value, (TValue?)value),
        SelectAsync,
        Disabled,
        Required,
        Invalid,
        EffectiveName);

    private async Task SelectAsync(object? value)
    {
        if (!Disabled && ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync((TValue?)value);
    }
}
