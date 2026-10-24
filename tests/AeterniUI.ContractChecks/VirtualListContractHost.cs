using AeterniUI.Components.VirtualList;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Hosts a <see cref="VirtualList{TItem}" /> so the contract checks can drive its
/// browser-facing callbacks directly and read back the controlled state, the way
/// <see cref="ListContractHost" /> does for <c>List</c>. Selection is bound through
/// the typed API by default; set <see cref="LegacyApi"/> to cover the object-typed
/// compatibility members.
/// </summary>
internal sealed class VirtualListContractHost : ComponentBase
{
    [Parameter] public Action<VirtualListContractHost>? Ready { get; set; }

    public IReadOnlyList<string> Items { get; set; } = ["Alpha", "Beta", "Gamma"];
    public SelectionMode Mode { get; set; } = SelectionMode.Single;
    public Func<string, object?>? KeySelector { get; set; }
    public Func<string, bool>? DisabledSelector { get; set; }
    public bool HasMoreItems { get; set; }
    public bool Disabled { get; set; }
    public bool AllowClear { get; set; }

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

    /// <summary>Number of times the component asked the host for more items.</summary>
    public int LoadMoreCalls { get; private set; }

    /// <summary>
    /// When set, the host holds its OnLoadMore callback open until the gate
    /// completes, so a test can observe the component's in-flight behaviour.
    /// </summary>
    public TaskCompletionSource? LoadMoreGate { get; set; }

    /// <summary>Values reported through <c>OnItemSelected</c>, in order.</summary>
    public List<string> Picked { get; } = [];

    public VirtualList<string> List { get; private set; } = null!;

    protected override void OnInitialized() => Ready?.Invoke(this);

    public void Update() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<VirtualList<string>>(0);
        builder.AddAttribute(1, "Items", Items);
        builder.AddAttribute(2, "SelectionMode", Mode);
        builder.AddAttribute(3, "Disabled", Disabled);
        builder.AddAttribute(4, "AllowClear", AllowClear);
        builder.AddAttribute(5, "ItemKeySelector", KeySelector);
        builder.AddAttribute(6, "DisabledSelector", DisabledSelector);
        builder.AddAttribute(7, "HasMoreItems", HasMoreItems);
        builder.AddAttribute(8, "OnLoadMore", EventCallback.Factory.Create(this, async () =>
        {
            LoadMoreCalls++;
            if (LoadMoreGate is not null)
            {
                await LoadMoreGate.Task;
            }
        }));

        if (LegacyApi)
        {
            builder.AddAttribute(9, "SelectedValue", Selected);
            builder.AddAttribute(10, "SelectedValueChanged", EventCallback.Factory.Create<object?>(this, value =>
            {
                Selected = value;
                SingleChanges += 1;
            }));
            builder.AddAttribute(11, "SelectedValues", SelectedMany);
            builder.AddAttribute(12, "SelectedValuesChanged", EventCallback.Factory.Create<IReadOnlyList<object?>>(this, values =>
            {
                SelectedMany = values;
                MultipleChanges += 1;
            }));
        }
        else
        {
            builder.AddAttribute(9, "SelectedItem", (string?)Selected);
            builder.AddAttribute(10, "SelectedItemChanged", EventCallback.Factory.Create<string?>(this, value =>
            {
                Selected = value;
                SingleChanges += 1;
            }));
            builder.AddAttribute(11, "SelectedItems", SelectedMany.Cast<string>().ToArray());
            builder.AddAttribute(12, "SelectedItemsChanged", EventCallback.Factory.Create<IReadOnlyList<string>>(this, values =>
            {
                SelectedMany = values.Cast<object?>().ToArray();
                MultipleChanges += 1;
            }));
        }

        builder.AddAttribute(13, "OnItemSelected", EventCallback.Factory.Create<object?>(this, value => Picked.Add((string)value!)));
        builder.AddComponentReferenceCapture(14, component => List = (VirtualList<string>)component);
        builder.CloseComponent();
    }
}
