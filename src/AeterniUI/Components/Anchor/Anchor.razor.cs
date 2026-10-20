using AeterniUI.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Anchor;

/// <summary>A single destination in an <see cref="Anchor"/> section navigation.</summary>
/// <param name="Id">Id of the section element the link points at. The host renders that element.</param>
/// <param name="Title">Visible link text.</param>
public sealed record AnchorLink(string Id, string Title);

/// <summary>
/// In-page navigation for long pages. The component renders only the link list; the
/// sections themselves belong to the host. The browser observes the section
/// positions to report the current one, and a click scrolls the document or the
/// host-provided scroll container without going through routing.
/// </summary>
/// <remarks>
/// <para>
/// The current item is a read-only projection of the scroll position: it is derived
/// in JavaScript and reported back through <see cref="OnActiveSectionChangedAsync"/>,
/// so there is no controlled "active value" parameter.
/// </para>
/// <para>
/// No navigation happens through <c>NavigationManager</c>. When
/// <see cref="UpdateLocation"/> is enabled the module only replaces the URL fragment
/// with <c>history.replaceState</c>, which neither creates a history entry nor
/// triggers a Blazor route update. Without JavaScript the rendered
/// <c>href="#id"</c> links keep the native anchor behaviour.
/// </para>
/// </remarks>
[JsModule("Components/Anchor/Anchor.razor.js", Name = "anchor", Interactive = true)]
public partial class Anchor : AeterniComponent
{
    /// <summary>Destinations in reading order. Each id must be unique and match a section the host renders.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<AnchorLink> Items { get; set; } = [];

    /// <summary>Id of the scroll container the sections live in. Empty means the document scrolls.</summary>
    [Parameter]
    public string? ContainerId { get; set; }

    /// <summary>Space in pixels left above the target section, e.g. for a sticky page header. Negative values are treated as zero.</summary>
    [Parameter]
    public int Offset { get; set; }

    /// <summary>Whether a click replaces the URL fragment with <c>history.replaceState</c> instead of adding a history entry.</summary>
    [Parameter]
    public bool UpdateLocation { get; set; } = true;

    /// <summary>Whether clicks scroll smoothly. The reduced-motion preference always wins and scrolls instantly.</summary>
    [Parameter]
    public bool Smooth { get; set; } = true;

    /// <summary>Accessible name of the navigation region. Defaults to <c>UiText.AnchorLabel</c>.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>The section the browser last reported as current, or null when none is reached.</summary>
    private string? _activeId;

    private string EffectiveAriaLabel =>
        string.IsNullOrWhiteSpace(AriaLabel) ? UiText.AnchorLabel : AriaLabel.Trim();

    private int EffectiveOffset => Math.Max(0, Offset);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Items is null)
        {
            throw new ArgumentNullException(nameof(Items));
        }

        if (Items.Any(item => item is null))
        {
            throw new ArgumentException("Anchor links cannot be null.", nameof(Items));
        }

        if (Items.Any(item => string.IsNullOrWhiteSpace(item.Id)))
        {
            throw new ArgumentException("Anchor link ids cannot be empty.", nameof(Items));
        }

        if (Items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != Items.Count)
        {
            throw new ArgumentException("Anchor link ids must be unique.", nameof(Items));
        }

        if (Items.Any(item => string.IsNullOrWhiteSpace(item.Title)))
        {
            throw new ArgumentException("Anchor link titles cannot be empty.", nameof(Items));
        }

        // A refreshed item list may no longer contain the section the browser last
        // reported; a stale marker would point aria-current at a link that is gone.
        if (_activeId is not null && !Items.Any(item => string.Equals(item.Id, _activeId, StringComparison.Ordinal)))
        {
            _activeId = null;
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-anchor");

    protected override Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (IsDisposed || !RootElementInitialized || !Visible)
        {
            return Task.CompletedTask;
        }

        // Positional-looking options are passed as a dictionary so the keys reach
        // JavaScript unchanged (dictionary keys are not renamed by the serializer).
        return JsModuleManager.InvokeModuleVoidAsync(
            "anchor",
            "attach",
            InstanceId,
            RootElement,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ids"] = Items.Select(item => item.Id).ToArray(),
                ["container"] = string.IsNullOrWhiteSpace(ContainerId) ? null : ContainerId.Trim(),
                ["offset"] = EffectiveOffset,
                ["smooth"] = Smooth,
                ["updateLocation"] = UpdateLocation
            });
    }

    /// <summary>
    /// Receives the current section id from the browser's scroll/intersection
    /// observation. An unknown or empty id clears the current marker.
    /// </summary>
    [JSInvokable]
    public Task OnActiveSectionChangedAsync(string? id)
    {
        if (IsDisposed)
        {
            return Task.CompletedTask;
        }

        // Only ids this instance actually renders may become current. The browser
        // can report a section id that a fresh parameter set already removed.
        var normalized = string.IsNullOrWhiteSpace(id)
            ? null
            : Items.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))?.Id;

        if (string.Equals(normalized, _activeId, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        _activeId = normalized;
        RequestStateHasChanged();
        return Task.CompletedTask;
    }

    private bool IsActive(AnchorLink item) => string.Equals(item.Id, _activeId, StringComparison.Ordinal);
}
