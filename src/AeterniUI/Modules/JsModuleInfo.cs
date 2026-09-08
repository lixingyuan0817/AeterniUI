namespace AeterniUI.Modules;

public sealed class JsModuleInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public bool Interactive { get; init; }
    public bool IsDependency { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; } = [];
}
