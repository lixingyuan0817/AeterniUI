using System.Globalization;

namespace AeterniUI.Components;

/// <summary>
/// Builds normalized inline CSS declarations for a component.
/// </summary>
public sealed class StyleBuilder
{
    private readonly List<string> _styles = [];

    public StyleBuilder()
    {
    }

    public StyleBuilder(params string?[] styles)
    {
        AddRange(styles);
    }

    /// <summary>
    /// Adds a raw CSS declaration when <paramref name="condition" /> is true.
    /// </summary>
    public StyleBuilder Add(string? declaration, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(declaration))
        {
            return this;
        }

        var normalizedDeclaration = declaration.Trim().TrimEnd(';').Trim();
        if (normalizedDeclaration.Length > 0)
        {
            _styles.Add(normalizedDeclaration);
        }

        return this;
    }

    public StyleBuilder AddStyle(string? declaration, bool condition = true) => Add(declaration, condition);

    public StyleBuilder Add(string property, object? value, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(property) || value is null)
        {
            return this;
        }

        var propertyName = property.Trim();
        var propertyValue = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(propertyValue)
            ? this
            : Add($"{propertyName}: {propertyValue}");
    }

    public StyleBuilder AddIf(bool condition, string? declaration) => Add(declaration, condition);

    public StyleBuilder AddRange(IEnumerable<string?>? styles, bool condition = true)
    {
        if (!condition || styles is null)
        {
            return this;
        }

        foreach (var style in styles)
        {
            Add(style);
        }

        return this;
    }

    public string Build() => _styles.Count == 0 ? string.Empty : $"{string.Join("; ", _styles)};";

    public override string ToString() => Build();
}
