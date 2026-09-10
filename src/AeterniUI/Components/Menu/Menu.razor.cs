using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Menu;

public sealed record MenuItem(string Id, string Label, string? Description = null, RenderFragment? Icon = null, bool Disabled = false);
public sealed record MenuGroup(string Key, string Label, IReadOnlyList<MenuItem> Items, RenderFragment? Icon = null, bool InitiallyOpen = true);

public partial class Menu : AeterniComponent
{
    [Parameter, EditorRequired] public IReadOnlyList<MenuGroup> Items { get; set; } = [];
    [Parameter] public string? SelectedId { get; set; }
    [Parameter] public EventCallback<string> SelectedIdChanged { get; set; }
    [Parameter] public EventCallback<MenuItem> ItemSelected { get; set; }
    [Parameter] public bool Accordion { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    private readonly HashSet<string> _open = [];
    private bool _initialized;

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-menu").Add("is-accordion", Accordion);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_initialized) { foreach (var group in Items.Where(x => x.InitiallyOpen)) _open.Add(group.Key); _initialized = true; }
    }

    private bool IsOpen(string key) => _open.Contains(key);
    private async Task ToggleGroupAsync(string key)
    {
        if (_open.Contains(key)) _open.Remove(key);
        else { if (Accordion) _open.Clear(); _open.Add(key); }
        await InvokeAsync(StateHasChanged);
    }
    private async Task SelectAsync(MenuItem item)
    {
        if (item.Disabled) return;
        await SelectedIdChanged.InvokeAsync(item.Id);
        await ItemSelected.InvokeAsync(item);
    }
}
