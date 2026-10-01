using AeterniUI.Components.FormField;
using AeterniUI.Components.Slider;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Hosts a <see cref="Slider{TValue}" /> inside a <see cref="FormField" /> so the
/// contract checks can assert the container-style field linkage and drive the
/// pointer and keyboard paths against the live instance.
/// </summary>
internal sealed class SliderContractHost : ComponentBase
{
    [Parameter]
    public Action<SliderContractHost>? Ready { get; set; }

    public int Value = 25;
    public bool Disabled;
    public bool Invalid;
    public int Min = 0;
    public int Max = 100;
    public int Step = 5;
    public Orientation Orientation = Orientation.Horizontal;

    /// <summary>Values reported through <c>ValueChanged</c>, in order.</summary>
    public List<int> Changes { get; } = [];

    public Slider<int> Field { get; private set; } = null!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FormField>(0);
        builder.AddAttribute(1, "Label", "Volume");
        builder.AddAttribute(2, "Description", "Output level");
        builder.AddAttribute(3, "Disabled", Disabled);
        builder.AddAttribute(4, "Invalid", Invalid);
        builder.AddAttribute(5, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<Slider<int>>(0);
            content.AddAttribute(1, "Min", Min);
            content.AddAttribute(2, "Max", Max);
            content.AddAttribute(3, "Step", Step);
            content.AddAttribute(4, "Value", Value);
            content.AddAttribute(7, "Orientation", Orientation);
            content.AddAttribute(5, "ValueChanged", EventCallback.Factory.Create<int>(this, value =>
            {
                // A controlled host writes the value back so the echo render feeds
                // the same value down again, exactly like @bind-Value would.
                Value = value;
                Changes.Add(value);
            }));
            content.AddComponentReferenceCapture(6, component => Field = (Slider<int>)component);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
