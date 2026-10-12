namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// Pieces of the API table that every component page shares.
/// </summary>
/// <remarks>
/// The base parameters (<c>Id</c>, <c>Disabled</c>, …) are documented once in
/// <c>delivered-features</c> and must appear on every page, but they belong to no component
/// source file, so the generator cannot produce them. They are merged in at render time
/// instead of being repeated for all 53 pages in the generated artifact.
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

    public static IReadOnlyList<ShowcaseApiMember> BaseMembers { get; } = CommonParameters
        .Select(parameter => new ShowcaseApiMember(
            parameter.Name,
            parameter.Type,
            parameter.DefaultValue ?? string.Empty,
            parameter.Description,
            "属性",
            string.Empty))
        .ToArray();
}
