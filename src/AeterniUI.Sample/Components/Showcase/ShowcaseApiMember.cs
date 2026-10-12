namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// One row of a component page's API table.
/// </summary>
/// <remarks>
/// The rows are generated from the library source (<c>scripts/generate-api-reference.mjs</c>)
/// rather than written per page: a hand-written table drifts silently, and the review
/// measured eight pages whose tables no longer matched the components. The editorial parts
/// — the prose in <see cref="Description"/> and the rows a generator cannot derive (models,
/// services, umbrella rows such as <c>Value / StartDate / EndDate</c>) — live in
/// <c>scripts/api-descriptions.json</c> and <c>scripts/api-extra-rows.json</c>.
/// </remarks>
/// <param name="Name">Member name as it appears on the component.</param>
/// <param name="Type">Declared type, or a short signature for an interface row.</param>
/// <param name="DefaultValue">Literal default, empty when the initializer is not worth printing.</param>
/// <param name="Description">What the member does. Empty when nobody wrote it yet.</param>
/// <param name="Kind">Grading used for grouping: 属性, 事件回调 or 接口.</param>
/// <param name="Since">Version that introduced the member, empty when unknown.</param>
public sealed record ShowcaseApiMember(
    string Name,
    string Type,
    string DefaultValue,
    string Description,
    string Kind,
    string Since);
