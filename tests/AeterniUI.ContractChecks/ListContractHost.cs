using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AeterniUI.Enums;

internal sealed class ListContractHost : ComponentBase
{
    [Parameter] public Action<ListContractHost>? Ready { get; set; }
    public string?[] Rows { get; set; } = ["Alpha", "Beta", "Gamma"];
    public bool Disabled { get; set; }
    public bool Declarative { get; set; }
    public bool RowDisabled { get; set; }
    public bool AllowClear { get; set; }
    public bool Keyed { get; set; }
    public string? HiddenRow { get; set; }
    public SelectionMode Mode { get; set; } = SelectionMode.Single;
    public object? Selected { get; set; }
    public IReadOnlyList<object?> SelectedMany { get; set; } = [];
    public AeterniUI.Components.List.List<string?> List { get; private set; } = null!;

    protected override void OnInitialized() => Ready?.Invoke(this);
    public void Update() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<AeterniUI.Components.List.List<string?>>(0);
        builder.AddAttribute(1, "Items", Declarative ? null : Rows);
        builder.AddAttribute(2, "SelectionMode", Mode);
        builder.AddAttribute(3, "Disabled", Disabled);
        builder.AddAttribute(4, "SelectedValue", Selected);
        builder.AddAttribute(5, "SelectedValueChanged", EventCallback.Factory.Create<object?>(this, value => Selected = value));
        builder.AddAttribute(6, "SelectedValues", SelectedMany);
        builder.AddAttribute(7, "SelectedValuesChanged", EventCallback.Factory.Create<IReadOnlyList<object?>>(this, values => SelectedMany = values));
        if (Declarative)
        {
            builder.AddAttribute(8, "ChildContent", (RenderFragment)(content =>
            {
                foreach (var row in Rows)
                {
                    content.OpenComponent<AeterniUI.Components.List.ListItem<string?>>(0);
                    if (Keyed) content.SetKey(row);
                    content.AddAttribute(1, "Value", row);
                    content.AddAttribute(2, "Disabled", RowDisabled && row == Rows[0]);
                    content.AddAttribute(3, "ChildContent", (RenderFragment)(text => text.AddContent(0, row)));
                    content.AddAttribute(4, "Visible", row != HiddenRow);
                    content.CloseComponent();
                }
            }));
        }
        builder.AddAttribute(9, "AllowClear", AllowClear);
        builder.AddComponentReferenceCapture(10, component => List = (AeterniUI.Components.List.List<string?>)component);
        builder.CloseComponent();
    }
}
