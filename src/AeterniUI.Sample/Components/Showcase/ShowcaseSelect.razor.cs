using Microsoft.AspNetCore.Components;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// Sample-only adapter that lets the showcase use the library ComboBox for
/// enum and value-type properties. ComboBox itself intentionally accepts
/// reference-type items, so this adapter wraps each value in an option object.
/// </summary>
public partial class ShowcaseSelect<TValue> : ComponentBase
{
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public IReadOnlyList<TValue> Options { get; set; } = Array.Empty<TValue>();

    [Parameter]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public Func<TValue, string>? TextSelector { get; set; }

    private IReadOnlyList<ShowcaseSelectOption<TValue>> _options = Array.Empty<ShowcaseSelectOption<TValue>>();
    private ShowcaseSelectOption<TValue>? _selectedOption;

    protected override void OnParametersSet()
    {
        _options = Options
            .Select(value => new ShowcaseSelectOption<TValue>(
                value,
                TextSelector?.Invoke(value) ?? (value is null ? string.Empty : value.ToString() ?? string.Empty)))
            .ToArray();

        _selectedOption = _options.FirstOrDefault(option =>
            EqualityComparer<TValue>.Default.Equals(option.Value, Value));
    }

    private string GetOptionText(ShowcaseSelectOption<TValue> option) => option.Label;

    private async Task HandleValueChanged(ShowcaseSelectOption<TValue>? option)
    {
        if (option is null)
        {
            return;
        }

        Value = option.Value;
        await ValueChanged.InvokeAsync(option.Value);
    }
}

public sealed record ShowcaseSelectOption<TValue>(TValue Value, string Label);
