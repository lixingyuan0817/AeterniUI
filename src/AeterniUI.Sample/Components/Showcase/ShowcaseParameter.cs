namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// One row of the base-parameter block that every component page's API table starts with.
/// </summary>
/// <remarks>
/// These rows describe <c>AeterniComponent</c>, so no component source file owns them and
/// <c>scripts/generate-api-reference.mjs</c> cannot derive them. The rows themselves live in
/// <see cref="ShowcaseApi.CommonParameters"/> and are converted to <see cref="ShowcaseApiMember"/>
/// rows when a page renders.
/// </remarks>
/// <param name="Name">Member name as it appears on the component.</param>
/// <param name="Type">Declared type, or a short signature for an interface row.</param>
/// <param name="Description">What the member does.</param>
/// <param name="DefaultValue">Literal default, empty when the initializer is not worth printing.</param>
public sealed record ShowcaseParameter(
    string Name,
    string Type,
    string Description,
    string? DefaultValue = null);
