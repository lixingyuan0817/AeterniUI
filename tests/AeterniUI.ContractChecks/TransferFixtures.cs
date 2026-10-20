using AeterniUI.Components.FormField;
using AeterniUI.Components.Transfer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>一行候选数据，供 Transfer 契约检查使用；禁用状态由数据本身携带。</summary>
internal sealed record TransferItem(string Label, bool Disabled = false)
{
    public override string ToString() => Label;
}

/// <summary>
/// 两套数据：四行候选里有一行始终禁用，另有一个不属于候选集的越界值，用来驱动
/// 「SelectedValues 必须是 Items 子集」的参数同步分支。
/// </summary>
internal static class TransferFixtures
{
    public static IReadOnlyList<TransferItem> Items { get; } =
    [
        new("Alpha"),
        new("Beta"),
        new("Gamma"),
        new("Blocked", Disabled: true)
    ];

    /// <summary>目标列的初始值；顺序（Gamma 在 Beta 前）用来断言目标列按 SelectedValues 渲染。</summary>
    public static IReadOnlyList<TransferItem> InitialSelected { get; } = [Items[2], Items[1]];

    public static TransferItem Stranger { get; } = new("Stranger");

    public static Func<TransferItem, string> Text { get; } = item => item.Label;

    public static Func<TransferItem, bool> Disabled { get; } = item => item.Disabled;

    public static TransferItem Find(string label) => Items.First(item => item.Label == label);
}

/// <summary>
/// A controlled <c>Transfer</c> with a receiver for <see cref="Transfer{TItem}.SelectedValuesChanged"/>.
/// The checks drive the component's private interaction entry points and then re-render the
/// host, the same shape the Cascader and MultiSelect checks use.
/// </summary>
internal sealed class TransferContractHost : ComponentBase
{
    [Parameter]
    public Action<TransferContractHost>? Ready { get; set; }

    public IReadOnlyList<TransferItem> Values { get; private set; } = [];

    public List<IReadOnlyList<TransferItem>> Changes { get; } = [];

    public Transfer<TransferItem> Inner { get; private set; } = default!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Transfer<TransferItem>>(0);
        builder.AddAttribute(1, nameof(Transfer<TransferItem>.Items), TransferFixtures.Items);
        builder.AddAttribute(2, nameof(Transfer<TransferItem>.SelectedValues), Values);
        builder.AddAttribute(3, nameof(Transfer<TransferItem>.SelectedValuesChanged),
            EventCallback.Factory.Create<IReadOnlyList<TransferItem>>(this, next =>
            {
                Values = next;
                Changes.Add(next);
            }));
        builder.AddAttribute(4, nameof(Transfer<TransferItem>.TextSelector), TransferFixtures.Text);
        builder.AddAttribute(5, nameof(Transfer<TransferItem>.DisabledSelector), TransferFixtures.Disabled);
        builder.AddAttribute(6, nameof(Transfer<TransferItem>.AriaLabel), "Permissions");
        builder.AddComponentReferenceCapture(7, component => Inner = (Transfer<TransferItem>)component);
        builder.CloseComponent();
    }
}

/// <summary>
/// Starts with a value that is not in <c>Items</c>: the parameter sync must drop it and
/// report the retained subset through <c>SelectedValuesChanged</c>.
/// </summary>
internal sealed class TransferSubsetHost : ComponentBase
{
    [Parameter]
    public Action<TransferSubsetHost>? Ready { get; set; }

    public IReadOnlyList<TransferItem> Values { get; private set; } =
        [TransferFixtures.Find("Beta"), TransferFixtures.Stranger];

    public List<IReadOnlyList<TransferItem>> Changes { get; } = [];

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Transfer<TransferItem>>(0);
        builder.AddAttribute(1, nameof(Transfer<TransferItem>.Items), TransferFixtures.Items);
        builder.AddAttribute(2, nameof(Transfer<TransferItem>.SelectedValues), Values);
        builder.AddAttribute(3, nameof(Transfer<TransferItem>.SelectedValuesChanged),
            EventCallback.Factory.Create<IReadOnlyList<TransferItem>>(this, next =>
            {
                Values = next;
                Changes.Add(next);
            }));
        builder.AddAttribute(4, nameof(Transfer<TransferItem>.TextSelector), TransferFixtures.Text);
        builder.CloseComponent();
    }
}

/// <summary>
/// Wraps a <c>Transfer</c> in a <see cref="FormField"/> so the checks can assert the
/// cascaded disabled/required/invalid/description semantics without repeating a model.
/// </summary>
internal sealed class TransferFormHost : ComponentBase
{
    [Parameter]
    public Action<TransferFormHost>? Ready { get; set; }

    public bool Disabled;
    public bool Invalid;
    public bool Required;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FormField>(0);
        builder.AddAttribute(1, nameof(FormField.Label), "Permissions");
        builder.AddAttribute(2, nameof(FormField.Description), "Choose who may edit this record.");
        builder.AddAttribute(3, nameof(FormField.Required), Required);
        builder.AddAttribute(4, nameof(FormField.Invalid), Invalid);
        builder.AddAttribute(5, nameof(FormField.Disabled), Disabled);
        builder.AddAttribute(6, nameof(FormField.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<Transfer<TransferItem>>(0);
            content.AddAttribute(1, nameof(Transfer<TransferItem>.Items), TransferFixtures.Items);
            content.AddAttribute(2, nameof(Transfer<TransferItem>.SelectedValues), TransferFixtures.InitialSelected);
            content.AddAttribute(3, nameof(Transfer<TransferItem>.TextSelector), TransferFixtures.Text);
            content.AddAttribute(4, nameof(Transfer<TransferItem>.DisabledSelector), TransferFixtures.Disabled);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
