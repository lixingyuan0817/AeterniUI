using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AeterniUI.Enums;

/// <summary>
/// Hosts a <c>List&lt;string?&gt;</c> so the contract checks can drive its
/// browser-facing callbacks directly. Selection is bound through the typed
/// <c>SelectedItem</c>/<c>SelectedItems</c> API by default; set
/// <see cref="LegacyApi"/> to cover the object-typed compatibility members.
/// </summary>
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

    /// <summary>Single selection as written back by the bound callback.</summary>
    public object? Selected { get; set; }

    /// <summary>Multiple selection as written back by the bound callback.</summary>
    public IReadOnlyList<object?> SelectedMany { get; set; } = [];

    /// <summary>
    /// When true, binds <c>SelectedValue</c>/<c>SelectedValues</c> instead of the
    /// typed members, so the migration bridge keeps its own contract coverage.
    /// </summary>
    [Parameter]
    public bool LegacyApi { get; set; }

    public int SingleChanges { get; private set; }
    public int MultipleChanges { get; private set; }

    public AeterniUI.Components.List.List<string?> List { get; private set; } = null!;

    protected override void OnInitialized() => Ready?.Invoke(this);
    public void Update() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<AeterniUI.Components.List.List<string?>>(0);
        builder.AddAttribute(1, "Items", Declarative ? null : Rows);
        builder.AddAttribute(2, "SelectionMode", Mode);
        builder.AddAttribute(3, "Disabled", Disabled);

        if (LegacyApi)
        {
            builder.AddAttribute(4, "SelectedValue", Selected);
            builder.AddAttribute(5, "SelectedValueChanged", EventCallback.Factory.Create<object?>(this, value =>
            {
                Selected = value;
                SingleChanges += 1;
            }));
            builder.AddAttribute(6, "SelectedValues", SelectedMany);
            builder.AddAttribute(7, "SelectedValuesChanged", EventCallback.Factory.Create<IReadOnlyList<object?>>(this, values =>
            {
                SelectedMany = values;
                MultipleChanges += 1;
            }));
        }
        else
        {
            builder.AddAttribute(4, "SelectedItem", (string?)Selected);
            builder.AddAttribute(5, "SelectedItemChanged", EventCallback.Factory.Create<string?>(this, value =>
            {
                Selected = value;
                SingleChanges += 1;
            }));
            builder.AddAttribute(6, "SelectedItems", SelectedMany.Cast<string?>().ToArray());
            builder.AddAttribute(7, "SelectedItemsChanged", EventCallback.Factory.Create<IReadOnlyList<string?>>(this, values =>
            {
                SelectedMany = values.Cast<object?>().ToArray();
                MultipleChanges += 1;
            }));
        }

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
