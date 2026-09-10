using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.List;

public partial class ListItem : AeterniComponent
{
    [CascadingParameter]
    private List? Owner { get; set; }

    [Parameter]
    public object? Value { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? LeadingContent { get; set; }

    [Parameter]
    public RenderFragment? TrailingContent { get; set; }

    private ItemHandle? _handle;

    internal bool Interactive => Owner?.IsSelectable == true;

    internal string? OptionId => Owner is null
        ? null
        : $"{Owner.ElementId}-option-{InstanceId}";

    internal bool Selected => Owner?.IsSelected(Value) == true;

    /// <summary>
    /// Indicates that this option is the one reported by the Listbox
    /// container through <c>aria-activedescendant</c>.
    /// </summary>
    internal bool Active => _handle is not null && Owner?.IsActive(_handle) == true;

    internal bool EffectiveDisabled => Disabled || (Owner?.Disabled ?? false);

    /// <summary>
    /// Re-renders this option. The List calls it when the active option moves so
    /// the keyboard cue stays visible without focus leaving the container.
    /// </summary>
    internal void Refresh() => _ = InvokeAsync(StateHasChanged);

    private string BuildItemClass()
    {
        var classes = new List<string> { "aeterni-list-item" };
        if (Selected)
        {
            classes.Add("is-selected");
        }

        if (Active)
        {
            classes.Add("is-active");
        }

        if (EffectiveDisabled)
        {
            classes.Add("is-disabled");
        }

        return string.Join(" ", classes);
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (Owner is null || EffectiveDisabled)
        {
            return;
        }

        Owner.SetActive(_handle);
        await Owner.ToggleAsync(_handle!);
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (Owner is null)
        {
            return Task.CompletedTask;
        }

        if (_handle is null)
        {
            _handle = new ItemHandle(this, Value, EffectiveDisabled);
            Owner.Register(_handle);
        }

        _handle.Disabled = EffectiveDisabled;
        _handle.OptionId = OptionId;

        return Task.CompletedTask;
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        if (_handle is not null)
        {
            Owner?.Unregister(_handle);
        }

        return ValueTask.CompletedTask;
    }
}
