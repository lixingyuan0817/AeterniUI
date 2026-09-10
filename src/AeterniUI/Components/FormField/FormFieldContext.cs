namespace AeterniUI.Components.FormField;

internal sealed record FormFieldContext(
    string InputId,
    string? LabelId,
    string? DescribedBy,
    bool Disabled,
    bool Invalid,
    bool Required);
