using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Accordion;

public sealed record AccordionItem(string Id, string Title, RenderFragment Content, bool Disabled = false);

public partial class Accordion : AeterniComponent
{
    [Parameter, EditorRequired] public IReadOnlyList<AccordionItem> Items { get; set; } = [];
    [Parameter] public bool Multiple { get; set; }
    [Parameter] public IReadOnlyList<string>? OpenKeys { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<string>> OpenKeysChanged { get; set; }
    [Parameter] public string? AriaLabel { get; set; }

    private readonly HashSet<string> _open = [];
    private bool _initialized;
    private bool IsControlled => OpenKeys is not null;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_initialized && !IsControlled)
        {
            _open.Clear();
            _initialized = true;
        }
        if (Items.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Title)))
            throw new ArgumentException("Accordion item ids and titles are required.");
        if (Items.GroupBy(item => item.Id).Any(group => group.Count() > 1))
            throw new ArgumentException("Accordion item ids must be unique.");
    }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-accordion");

    private bool IsOpen(string id) => IsControlled ? OpenKeys!.Contains(id) : _open.Contains(id);

    private async Task ToggleAsync(AccordionItem item)
    {
        if (item.Disabled) return;
        var next = IsOpen(item.Id);
        var keys = IsControlled ? OpenKeys!.ToHashSet() : _open.ToHashSet();
        if (next) keys.Remove(item.Id);
        else
        {
            if (!Multiple) keys.Clear();
            keys.Add(item.Id);
        }
        if (!IsControlled)
        {
            _open.Clear();
            _open.UnionWith(keys);
        }
        if (OpenKeysChanged.HasDelegate)
        {
            await OpenKeysChanged.InvokeAsync(keys.ToArray());
        }
    }
}
