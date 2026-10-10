namespace AeterniUI.Icons;

/// <summary>
/// Describes an inline SVG icon independently of an icon vendor.
/// </summary>
public sealed class IconDefinition
{
    public IconDefinition(string name, int width, int height, string path)
        : this(name, width, height, [path])
    {
    }

    public IconDefinition(string name, int width, int height, IEnumerable<string> paths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(paths);

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The SVG width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "The SVG height must be greater than zero.");
        }

        var normalizedPaths = paths
            .Where(pathData => !string.IsNullOrWhiteSpace(pathData))
            .Select(pathData => pathData.Trim())
            .ToArray();

        if (normalizedPaths.Length == 0)
        {
            throw new ArgumentException("At least one SVG path is required.", nameof(paths));
        }

        Name = name.Trim();
        Width = width;
        Height = height;
        Paths = Array.AsReadOnly(normalizedPaths);
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<string> Paths { get; }

    public string ViewBox => $"0 0 {Width} {Height}";
}
