using AeterniUI.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Tabs;

/// <summary>
/// Tab navigation with a <c>tablist</c>/<c>tab</c>/<c>tabpanel</c> model. The
/// component renders the tab strip from its registered <see cref="Tab" /> children;
/// each tab renders its own panel. Selection is controlled through
/// <see cref="Value" /> / <see cref="ValueChanged" /> and follows the roving
/// tabindex model: one Tab stop for the whole strip, arrow keys move (and activate)
/// inside it.
/// </summary>
[JsModule("Components/Tabs/Tabs.razor.js", Name = "tabs")]
public partial class Tabs : AeterniComponent
{
    private const string ModuleName = "tabs";

    /// <summary>Registered tabs, in markup order.</summary>
    private readonly List<Tab> _tabs = [];

    /// <summary>Registration index to focus after the next render, or -1.</summary>
    private int _pendingFocusIndex = -1;

    /// <summary>
    /// Value of the selected tab. Controlled: the component never mutates it without
    /// raising <see cref="ValueChanged" />, so <c>@bind-Value</c> stays authoritative.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Accessible name of the tab strip. Defaults to
    /// <see cref="AeterniUITextOptions.TabsLabel" />; a tab strip without a name is
    /// not announced as a group, so prefer passing a real label.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal IReadOnlyList<Tab> RegisteredTabs => _tabs;

    internal string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.TabsLabel : AriaLabel.Trim();

    internal bool IsSelected(Tab tab) => string.Equals(Value ?? string.Empty, tab.Value, StringComparison.Ordinal);

    /// <summary>
    /// The tab rendered as selected. When <see cref="Value" /> matches no tab — the
    /// initial null included — the first enabled tab is shown, so the simplest usage
    /// still renders a usable panel without the consumer setting a value first.
    /// </summary>
    internal Tab? SelectedTab => _tabs.FirstOrDefault(IsSelected) ?? FirstEnabledTab;

    /// <summary>True when this tab is the one rendered as selected.</summary>
    internal bool IsRenderedSelected(Tab tab) => ReferenceEquals(tab, SelectedTab);

    /// <summary>
    /// The single tab that carries <c>tabindex="0"</c>. Falls back to the first
    /// enabled tab so the strip stays reachable when the selected tab is disabled.
    /// </summary>
    internal Tab? TabStop => SelectedTab is { Disabled: false } ? SelectedTab : FirstEnabledTab;

    private Tab? FirstEnabledTab => _tabs.FirstOrDefault(tab => !tab.Disabled) ?? _tabs.FirstOrDefault();

    internal int IndexOf(Tab tab) => _tabs.IndexOf(tab);

    internal string TabButtonId(Tab tab) => $"{ElementId}-tab-{IndexOf(tab)}";

    internal string TabPanelId(Tab tab) => $"{ElementId}-panel-{IndexOf(tab)}";

    internal void Register(Tab tab)
    {
        if (_tabs.Any(existing => string.Equals(existing.Value, tab.Value, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Tabs already contains a tab with the value '{tab.Value}'. Tab values must be unique inside one Tabs instance.");
        }

        _tabs.Add(tab);

        // The strip is rendered by this component from the registered children, so it
        // has to render once more after the children registered themselves. Deferred
        // on purpose: Register runs while the current render batch is being built.
        _ = InvokeAsync(StateHasChanged);
    }

    internal void Unregister(Tab tab) => _tabs.Remove(tab);

    internal async Task SelectAsync(Tab tab, bool moveFocus = false)
    {
        if (tab.Disabled)
        {
            return;
        }

        if (moveFocus)
        {
            _pendingFocusIndex = IndexOf(tab);
        }

        if (!IsSelected(tab))
        {
            Value = tab.Value;

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
        }

        StateHasChanged();
    }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-tabs");

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RootElementInitialized)
        {
            await JsModuleManager.InvokeModuleVoidAsync(ModuleName, "attach", InstanceId, RootElement);
        }

        if (_pendingFocusIndex >= 0)
        {
            var index = _pendingFocusIndex;
            _pendingFocusIndex = -1;
            await JsModuleManager.InvokeModuleVoidAsync(ModuleName, "focusTab", InstanceId, index);
        }
    }

    /// <summary>
    /// Automatic activation model from the ARIA tabs pattern: the arrow keys both
    /// move the roving tabindex and select the tab they land on.
    /// </summary>
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is not ("ArrowRight" or "ArrowLeft" or "Home" or "End"))
        {
            return;
        }

        var enabled = _tabs.Where(tab => !tab.Disabled).ToList();
        if (enabled.Count == 0)
        {
            return;
        }

        var current = enabled.FindIndex(tab => ReferenceEquals(tab, SelectedTab));
        if (current < 0)
        {
            current = 0;
        }

        // Arrow keys follow the visual order, so they swap in RTL. The module reports
        // the writing direction and falls back to LTR when JS is unavailable.
        var isRtl = await JsModuleManager.InvokeModuleAsync<bool>(ModuleName, "isRtl", InstanceId);
        var forward = args.Key switch
        {
            "ArrowRight" => !isRtl,
            "ArrowLeft" => isRtl,
            _ => true
        };

        var target = args.Key switch
        {
            "Home" => 0,
            "End" => enabled.Count - 1,
            _ => (current + (forward ? 1 : -1) + enabled.Count) % enabled.Count
        };

        await SelectAsync(enabled[target], moveFocus: true);
    }
}
