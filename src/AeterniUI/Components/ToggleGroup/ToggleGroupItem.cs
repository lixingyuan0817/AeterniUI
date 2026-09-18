namespace AeterniUI.Components.ToggleGroup;

/// <summary>A named toggle action with a stable, ordinal identifier.</summary>
public sealed record ToggleGroupItem(string Id, string Text, bool Disabled = false);
