namespace AeterniUI.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class JsDependencyAttribute(string path) : Attribute
{
    public string Path { get; } = path;
    public string? Alias { get; set; }
}
