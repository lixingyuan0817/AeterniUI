using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.NavTree;

/// <summary>
/// One node in a <see cref="NavTree"/>. Children make the node expandable.
/// </summary>
public sealed record NavTreeNode(
    string Id,
    string Label,
    IReadOnlyList<NavTreeNode>? Children = null,
    RenderFragment? Icon = null,
    RenderFragment? Trailing = null,
    string? Description = null,
    bool Disabled = false,
    bool Visible = true)
{
    /// <summary>Indicates whether the node has at least one visible child.</summary>
    public bool HasChildren => Children?.Any(child => child.Visible) == true;
}
