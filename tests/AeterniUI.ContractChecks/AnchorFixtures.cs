using AeterniUI.Components.Anchor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>节段链接，供 Anchor 契约检查使用。</summary>
internal static class AnchorFixtures
{
    /// <summary>三个目标段：覆盖首项、中间项与末项的活动态回填。</summary>
    public static IReadOnlyList<AnchorLink> Sections { get; } =
    [
        new("intro", "Introduction"),
        new("install", "Installation"),
        new("usage", "Usage")
    ];
}

/// <summary>
/// Renders an <c>Anchor</c> and keeps the live instance so a check can drive the
/// browser callback (<c>OnActiveSectionChangedAsync</c>) and re-read the markup,
/// the same shape the Cascader and List hosts use.
/// </summary>
internal sealed class AnchorContractHost : ComponentBase
{
    [Parameter]
    public Action<AnchorContractHost>? Ready { get; set; }

    public Anchor Inner { get; private set; } = default!;

    public void Update() => StateHasChanged();

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Anchor>(0);
        builder.AddAttribute(1, nameof(Anchor.Items), AnchorFixtures.Sections);
        builder.AddAttribute(2, nameof(Anchor.ContainerId), "contract-anchor-scroll");
        builder.AddAttribute(3, nameof(Anchor.Offset), 56);
        builder.AddAttribute(4, nameof(Anchor.AriaLabel), "Contract sections");
        builder.AddComponentReferenceCapture(5, component => Inner = (Anchor)component);
        builder.CloseComponent();
    }
}
