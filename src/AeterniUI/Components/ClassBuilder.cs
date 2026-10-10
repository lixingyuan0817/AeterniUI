namespace AeterniUI.Components;

/// <summary>
/// Builds a normalized CSS class list for a component.
/// </summary>
public sealed class ClassBuilder
{
    private readonly List<string> _classes = [];
    private readonly HashSet<string> _knownClasses = new(StringComparer.Ordinal);

    public ClassBuilder()
    {
    }

    public ClassBuilder(params string?[] classes)
    {
        AddRange(classes);
    }

    /// <summary>
    /// Adds one or more whitespace-separated class names when <paramref name="condition" /> is true.
    /// </summary>
    public ClassBuilder Add(string? classes, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(classes))
        {
            return this;
        }

        foreach (var className in classes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (_knownClasses.Add(className))
            {
                _classes.Add(className);
            }
        }

        return this;
    }

    public ClassBuilder AddClass(string? classes, bool condition = true) => Add(classes, condition);

    public ClassBuilder AddIf(bool condition, string? classes) => Add(classes, condition);

    public ClassBuilder AddRange(IEnumerable<string?>? classes, bool condition = true)
    {
        if (!condition || classes is null)
        {
            return this;
        }

        foreach (var className in classes)
        {
            Add(className);
        }

        return this;
    }

    public string Build() => string.Join(' ', _classes);

    public override string ToString() => Build();
}
