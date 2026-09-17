namespace AeterniUI.Models;

/// <summary>A host-defined shortcut shown by <c>DateRangePicker</c>.</summary>
public sealed record DateRangePreset(string Label, DateOnly StartDate, DateOnly EndDate);
