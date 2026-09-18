using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.ToggleGroup;

/// <summary>Controlled action toggles; arrow navigation never changes pressed state.</summary>
[JsModule("Components/ToggleGroup/ToggleGroup.razor.js", Name = "toggle-group", Interactive = true)]
public partial class ToggleGroup : AeterniComponent
{
    [Parameter, EditorRequired] public IReadOnlyList<ToggleGroupItem> Items { get; set; } = [];
    [Parameter, EditorRequired] public string AriaLabel { get; set; } = string.Empty;
    [Parameter] public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;
    [Parameter] public IReadOnlyList<string> SelectedValues { get; set; } = [];
    [Parameter] public EventCallback<IReadOnlyList<string>> SelectedValuesChanged { get; set; }
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;
    [Parameter] public Size Size { get; set; } = Size.Default;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (SelectionMode is not (SelectionMode.Single or SelectionMode.Multiple))
            throw new ArgumentOutOfRangeException(nameof(SelectionMode));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (string.IsNullOrWhiteSpace(AriaLabel))
            throw new ArgumentException("A toggle group requires an accessible name.", nameof(AriaLabel));
        ArgumentNullException.ThrowIfNull(Items);
        ArgumentNullException.ThrowIfNull(SelectedValues);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Text) || !ids.Add(item.Id))
                throw new ArgumentException("Actions require unique nonempty IDs and nonempty names.", nameof(Items));
        }
        if (SelectionMode == SelectionMode.Single && SelectedValues.Count > 1)
            throw new ArgumentException("Single mode accepts at most one selected ID.", nameof(SelectedValues));
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in SelectedValues)
        {
            if (id is null || !ids.Contains(id) || !selected.Add(id))
                throw new ArgumentException("Selected IDs must be unique and present in Items.", nameof(SelectedValues));
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-toggle-group")
        .Add(ComponentClass.ForSize("aeterni-toggle-group", Size))
        .Add("aeterni-toggle-group--vertical", Orientation == Orientation.Vertical);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "group",
            ["aria-label"] = AriaLabel.Trim(),
            ["aria-disabled"] = Disabled ? "true" : "false",
            ["data-orientation"] = Orientation == Orientation.Vertical ? "vertical" : "horizontal",
            ["tabindex"] = "-1"
        };
        if (Disabled) attributes["inert"] = true;
        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("toggle-group", "sync", InstanceId, RootElement);

    private bool IsSelected(string id) => SelectedValues.Contains(id, StringComparer.Ordinal);

    private string ItemClass(ToggleGroupItem item) => ClassBuilder("aeterni-toggle-group__item")
        .Add("is-selected", IsSelected(item.Id)).Build();

    private Task ToggleAsync(string id)
    {
        var item = Items.FirstOrDefault(candidate => candidate.Id == id);
        if (!Visible || Disabled || item is null || item.Disabled) return Task.CompletedTask;
        IReadOnlyList<string> next = IsSelected(id)
            ? SelectedValues.Where(value => value != id).ToArray()
            : SelectionMode == SelectionMode.Single ? [id] : [.. SelectedValues, id];
        // The parent owns state, including rejecting or delaying this proposal.
        return SelectedValuesChanged.InvokeAsync(next);
    }
}
