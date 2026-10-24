namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// Pieces of the API table that every component page shares.
/// </summary>
/// <remarks>
/// The base parameters (<c>Id</c>, <c>Disabled</c>, …) are documented once in
/// <c>delivered-features</c> and belong to no component source file, so the generator cannot
/// read their descriptions from source. The rows themselves live in
/// <see cref="CommonParameters"/>; <em>which</em> of them actually reach a component's root
/// element is generated per component by <c>scripts/generate-api-reference.mjs</c> (from
/// <c>scripts/lib/showcase-support.mjs</c>) because it depends on whether the component renders
/// a DOM root, binds <c>@ref</c>, reads <c>Disabled</c>, and so on.
/// </remarks>
public static class ShowcaseApi
{
    public static IReadOnlyList<ShowcaseParameter> CommonParameters { get; } =
    [
        new("Id", "string?", "覆盖根元素的 DOM id。"),
        new("Disabled", "bool", "组件是否处于禁用状态；仅支持该语义的组件会应用它。", "false"),
        new("Visible", "bool", "是否渲染组件；false 时输出 hidden。", "true"),
        new("Class", "string?", "追加到根元素的 class。"),
        new("Style", "string?", "追加到根元素的 inline style。"),
        new("Element", "ElementReference?", "提供或接收根元素引用。"),
        new("AdditionalAttributes", "IReadOnlyDictionary<string, object>?", "透传未匹配的 HTML 属性。"),
        new("ElementChanged", "EventCallback<ElementReference>", "根元素引用发生变化时触发。")
    ];

    /// <summary>Kind grading for a base parameter row (same words as the generated rows).</summary>
    public static string KindOf(ShowcaseParameter parameter) =>
        parameter.Type.Contains("EventCallback", StringComparison.Ordinal) ? "事件回调" : "属性";
}

/// <summary>
/// One secondary group of a page's API table. Multi-component pages (DatePicker /
/// DateRangePicker, Tabs / Tab, PopupHost / Popover, Dialog / Confirm / Alert / Toast, …)
/// render one group per component type instead of a single mixed table.
/// </summary>
/// <param name="Title">Component (or owning type) the group documents.</param>
/// <param name="UnsupportedBaseParameters">
/// Base parameters that do not take effect for this component, with the reason. Empty means
/// all <c>AeterniComponent</c> parameters reach the root element; <c>null</c> means the
/// group is not a library component (models, services, shared conventions) and must not list
/// base parameters at all.
/// </param>
/// <param name="Members">Member names rendered in this group, in display order.</param>
public sealed record ShowcaseApiSection(
    string Title,
    IReadOnlyList<ShowcaseApiBaseParameter>? UnsupportedBaseParameters,
    IReadOnlyList<string> Members);

/// <summary>
/// A base parameter that does not apply to a component, plus the structural reason
/// (no DOM root, no <c>@ref</c> binding, parameter never read, …).
/// </summary>
public sealed record ShowcaseApiBaseParameter(string Name, string Note);

/// <summary>A rendered API row plus whether the public parameter actually takes effect.</summary>
public sealed record ShowcaseApiRow(ShowcaseApiMember Member, bool Supported, string Note);
