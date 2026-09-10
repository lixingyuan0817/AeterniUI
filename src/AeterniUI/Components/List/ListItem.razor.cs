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
    /// Click handler that is only bound while the owning List is selectable. A
    /// default <see cref="EventCallback{T}" /> renders no attribute at all, so a
    /// static row registers no DOM listener.
    /// </summary>
    private EventCallback<MouseEventArgs> ClickHandler => Interactive
        ? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync)
        : default;

    /// <summary>
    /// Re-renders this option. The List calls it when the active option moves so
    /// the keyboard cue stays visible without focus leaving the container.
    /// </summary>
    internal void Refresh() => _ = InvokeAsync(StateHasChanged);

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-list-item")
            .Add("is-selected", Selected)
            .Add("is-active", Active)
            .Add("is-disabled", EffectiveDisabled);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (!Interactive)
        {
            return attributes;
        }

        // The Listbox container points aria-activedescendant at this option, so the
        // option id takes over from the base element id while it is selectable.
        if (OptionId is not null)
        {
            attributes["id"] = OptionId;
        }

        attributes["role"] = "option";
        // ARIA state values are strings: a bool value renders as a minimised
        // attribute, which assistive technology reads as an invalid/empty value.
        attributes["aria-selected"] = Selected ? "true" : "false";

        if (EffectiveDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
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
