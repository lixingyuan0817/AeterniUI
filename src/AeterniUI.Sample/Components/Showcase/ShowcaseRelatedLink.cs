namespace AeterniUI.Sample.Components.Showcase;

/// <summary>One entry in a page's "相关组件" list.</summary>
/// <remarks>
/// Market component docs end a page with the components a reader is likely to need next
/// (a label with its input, a form field with its controls). Without it the only way to find
/// the neighbour is the sidebar.
/// </remarks>
public sealed record ShowcaseRelatedLink(string Label, string Href);
