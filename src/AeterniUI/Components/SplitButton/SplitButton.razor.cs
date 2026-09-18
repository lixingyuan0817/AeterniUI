using AeterniUI.Components.ButtonGroup;
using AeterniUI.Components.Menu;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.SplitButton;

/// <summary>A primary action paired with an independently named menu action.</summary>
public partial class SplitButton : AeterniComponent
{
    [CascadingParameter] private ButtonGroupContext? GroupContext { get; set; }

    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter, EditorRequired] public string MenuAriaLabel { get; set; } = string.Empty;
    [Parameter, EditorRequired] public IReadOnlyList<MenuGroup> Items { get; set; } = [];
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Ghost;
    [Parameter] public ButtonIntent Intent { get; set; } = ButtonIntent.Neutral;
    [Parameter] public Size Size { get; set; } = Size.Small;
    [Parameter] public bool PrimaryDisabled { get; set; }
    [Parameter] public bool MenuDisabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool FullWidth { get; set; }
    [Parameter] public PopupPlacement Placement { get; set; } = PopupPlacement.BottomEnd;
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MenuItem> OnItemSelected { get; set; }

    private bool _internalOpen;
    private bool IsDisabled => Disabled || GroupContext?.Disabled == true;
    private bool IsMenuDisabled => IsDisabled || MenuDisabled;
    private bool EffectiveOpen => !IsMenuDisabled && (OpenChanged.HasDelegate ? Open : _internalOpen);
    private string PrimaryId => $"{ElementId}-primary";
    private string MenuButtonId => $"{ElementId}-actions";
    private SplitButtonContext Context => new(PrimaryId, $"{MenuButtonId}-trigger");

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.IsNullOrWhiteSpace(Label))
            throw new ArgumentException("SplitButton requires a visible primary label.", nameof(Label));
        if (string.IsNullOrWhiteSpace(MenuAriaLabel))
            throw new ArgumentException("SplitButton requires a menu accessible name.", nameof(MenuAriaLabel));
        if (IsMenuDisabled)
            _internalOpen = false;
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-split-button")
        .Add("is-full-width", FullWidth)
        .Add("is-disabled", IsDisabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (IsDisabled)
            attributes["aria-disabled"] = "true";
        return attributes;
    }

    private Task HandleClickAsync(MouseEventArgs args) => IsDisabled || PrimaryDisabled || Loading
        ? Task.CompletedTask : OnClick.InvokeAsync(args);

    private Task HandleItemSelectedAsync(MenuItem item) => IsMenuDisabled
        ? Task.CompletedTask : OnItemSelected.InvokeAsync(item);

    private async Task HandleOpenChangedAsync(bool open)
    {
        open &= !IsMenuDisabled;
        if (OpenChanged.HasDelegate)
            await OpenChanged.InvokeAsync(open);
        else
            _internalOpen = open;
    }
}
