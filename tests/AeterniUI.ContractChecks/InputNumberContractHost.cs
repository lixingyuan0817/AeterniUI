using AeterniUI.Components.FormField;
using AeterniUI.Components.InputNumber;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Hosts an <see cref="InputNumber{TValue}" /> inside a <see cref="FormField" />
/// so the contract checks can assert the field-id adoption and the inherited
/// state, and can drive the private step and typing paths against the live
/// instance.
/// </summary>
internal sealed class InputNumberContractHost : ComponentBase
{
    [Parameter]
    public Action<InputNumberContractHost>? Ready { get; set; }

    public decimal? Value = 12.5m;
    public bool Disabled;
    public bool Invalid;
    public bool Required;

    /// <summary>Values reported through <c>ValueChanged</c>, in order.</summary>
    public List<decimal?> Changes { get; } = [];

    public InputNumber<decimal> Field { get; private set; } = null!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FormField>(0);
        builder.AddAttribute(1, "Label", "Amount");
        builder.AddAttribute(2, "Description", "Quantity to order");
        builder.AddAttribute(3, "Required", Required);
        builder.AddAttribute(4, "Disabled", Disabled);
        builder.AddAttribute(5, "Invalid", Invalid);
        builder.AddAttribute(6, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<InputNumber<decimal>>(0);
            content.AddAttribute(1, "Min", 0m);
            content.AddAttribute(2, "Max", 100m);
            content.AddAttribute(3, "Step", 0.5m);
            content.AddAttribute(4, "Value", Value);
            content.AddAttribute(5, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, value =>
            {
                // A controlled host writes the value back so the echo render feeds
                // the same value down again, exactly like @bind-Value would.
                Value = value;
                Changes.Add(value);
            }));
            content.AddComponentReferenceCapture(6, component => Field = (InputNumber<decimal>)component);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
