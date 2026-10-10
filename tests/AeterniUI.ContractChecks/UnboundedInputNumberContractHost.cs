using AeterniUI.Components.InputNumber;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Hosts unbounded <see cref="InputNumber{TValue}" /> shapes for the batch 23
/// overflow guards: an integer field with no bound, a decimal field at its
/// maximum, and a field whose Max does not sit on the precision grid.
/// </summary>
internal sealed class UnboundedInputNumberContractHost : ComponentBase
{
    [Parameter]
    public Action<UnboundedInputNumberContractHost>? Ready { get; set; }

    public int? IntValue = int.MaxValue;
    public decimal? DecimalValue = decimal.MaxValue;
    public decimal? GridValue = 0.3m;

    /// <summary>Every committed value, tagged with the field it came from.</summary>
    public List<string> Changes { get; } = [];

    public InputNumber<int> IntField { get; private set; } = null!;
    public InputNumber<decimal> DecimalField { get; private set; } = null!;
    public InputNumber<decimal> GridField { get; private set; } = null!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<InputNumber<int>>(0);
        builder.AddAttribute(1, "Value", IntValue);
        builder.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<int?>(this, value =>
        {
            IntValue = value;
            Changes.Add($"int:{value}");
        }));
        builder.AddComponentReferenceCapture(3, component => IntField = (InputNumber<int>)component);
        builder.CloseComponent();

        builder.OpenComponent<InputNumber<decimal>>(4);
        builder.AddAttribute(5, "Value", DecimalValue);
        builder.AddAttribute(6, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, value =>
        {
            DecimalValue = value;
            Changes.Add($"decimal:{value}");
        }));
        builder.AddComponentReferenceCapture(7, component => DecimalField = (InputNumber<decimal>)component);
        builder.CloseComponent();

        // Max is deliberately off the precision grid: 0.35 with Precision = 1.
        builder.OpenComponent<InputNumber<decimal>>(8);
        builder.AddAttribute(9, "Max", 0.35m);
        builder.AddAttribute(10, "Precision", 1);
        builder.AddAttribute(11, "Step", 0.1m);
        builder.AddAttribute(12, "Value", GridValue);
        builder.AddAttribute(13, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, value =>
        {
            GridValue = value;
            Changes.Add($"grid:{value}");
        }));
        builder.AddComponentReferenceCapture(14, component => GridField = (InputNumber<decimal>)component);
        builder.CloseComponent();
    }
}
