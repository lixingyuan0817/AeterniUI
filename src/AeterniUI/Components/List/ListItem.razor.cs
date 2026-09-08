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

    private ElementReference _rootElement;
    private ItemHandle? _handle;

    internal bool Interactive => Owner?.IsSelectable == true;

    internal string? OptionId => Owner is null
        ? null
        : $"{Owner.ElementId}-option-{InstanceId}";

    internal bool Selected => Owner?.IsSelected(Value) == true;

    internal bool EffectiveDisabled => Disabled || (Owner?.Disabled ?? false);

    private string BuildItemClass()
    {
        var classes = new List<string> { "aeterni-list-item" };
        if (Selected)
        {
            classes.Add("is-selected");
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

    private void HandleFocus()
    {
        Owner?.SetActive(_handle);
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (Owner is null)
        {
            return;
        }

        if (_handle is null)
        {
            _handle = new ItemHandle(this, Value, EffectiveDisabled);
            Owner.Register(_handle);
        }

        _handle.Element = _rootElement;
        _handle.Disabled = EffectiveDisabled;
        _handle.OptionId = OptionId;
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
