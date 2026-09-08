namespace AeterniUI.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class JsModuleAttribute(string path):Attribute
{
    public string Path { get; } = path;

    public string? Name { get; set; }

    public bool Interactive { get; set; }
}
