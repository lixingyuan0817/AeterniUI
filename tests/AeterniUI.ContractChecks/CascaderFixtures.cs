using AeterniUI.Components.Cascader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>层级节点，供 Cascader 契约检查使用。</summary>
internal sealed record CascaderNode(string Label, IReadOnlyList<CascaderNode> Children)
{
    public override string ToString() => Label;
}

/// <summary>两棵树：一棵三级（省/市/区），一棵只有一层（等价平铺）。</summary>
internal static class CascaderFixtures
{
    public static IReadOnlyList<CascaderNode> Tree { get; } =
    [
        new("Zhejiang", [
            new("Hangzhou", [new("Xihu", []), new("Binjiang", [])]),
            new("Ningbo", [new("Haishu", [])])
        ]),
        new("Jiangsu", [
            new("Nanjing", [new("Xuanwu", [])])
        ])
    ];

    public static IReadOnlyList<CascaderNode> Flat { get; } = [new("Alpha", []), new("Beta", [])];

    public static Func<CascaderNode, IEnumerable<CascaderNode>?> Children { get; } = node => node.Children;
}

/// <summary>
/// A controlled <c>Cascader</c> with a receiver for its callbacks. The checks drive
/// the private keyboard/interaction entry points and then re-render through the host,
/// the same shape the ComboBox and MultiSelect checks use.
/// </summary>
internal sealed class CascaderContractHost : ComponentBase
{
    [Parameter]
    public Action<CascaderContractHost>? Ready { get; set; }

    public CascaderNode? Value { get; private set; }

    public List<CascaderNode?> Changes { get; } = [];

    public List<CascaderNode?> Selections { get; } = [];

    public Cascader<CascaderNode> Inner { get; private set; } = default!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Cascader<CascaderNode>>(0);
        builder.AddAttribute(1, nameof(Cascader<CascaderNode>.Items), CascaderFixtures.Tree);
        builder.AddAttribute(2, nameof(Cascader<CascaderNode>.ChildrenSelector), CascaderFixtures.Children);
        builder.AddAttribute(3, nameof(Cascader<CascaderNode>.Value), Value);
        builder.AddAttribute(4, nameof(Cascader<CascaderNode>.ValueChanged), EventCallback.Factory.Create<CascaderNode?>(
            this,
            value =>
            {
                Value = value;
                Changes.Add(value);
            }));
        builder.AddAttribute(5, nameof(Cascader<CascaderNode>.OnItemSelected), EventCallback.Factory.Create<CascaderNode?>(
            this,
            value => Selections.Add(value)));
        builder.AddAttribute(6, nameof(Cascader<CascaderNode>.AriaLabel), "Region");
        builder.AddComponentReferenceCapture(7, component => Inner = (Cascader<CascaderNode>)component);
        builder.CloseComponent();
    }
}
