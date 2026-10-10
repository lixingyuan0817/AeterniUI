using AeterniUI.Components.FormField;
using AeterniUI.Components.MultiSelect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Hosts a <see cref="MultiSelect{TItem}" /> inside a <see cref="FormField" /> so
/// the contract checks can assert that the field label targets the trigger
/// (REV-136: the trigger used to render without an id, so `for` never resolved).
/// </summary>
internal sealed class MultiSelectContractHost : ComponentBase
{
    [Parameter]
    public Action<MultiSelectContractHost>? Ready { get; set; }

    public IEnumerable<string> Items = ["Design", "Development"];

    public IEnumerable<string> Selected = ["Design"];

    /// <summary>Host-supplied description id, merged with the field's own (REV-155).</summary>
    public string? AriaDescribedBy;

    public MultiSelect<string> Field { get; private set; } = null!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FormField>(0);
        builder.AddAttribute(1, "Label", "Categories");
        builder.AddAttribute(5, "Description", "Pick the categories to publish");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<MultiSelect<string>>(0);
            content.AddAttribute(1, "Items", Items);
            content.AddAttribute(5, "AriaDescribedBy", AriaDescribedBy);
            content.AddAttribute(2, "SelectedValues", Selected);
            content.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<IEnumerable<string>>(this, value =>
            {
                Selected = value;
            }));
            content.AddComponentReferenceCapture(4, component => Field = (MultiSelect<string>)component);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
