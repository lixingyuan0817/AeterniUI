using System.Globalization;
using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Dialog.Notice;

/// <summary>
/// Renders a single Alert or Toast notification card. Owned and placed by
/// <see cref="DialogProvider"/>; keeping the card templates and styles in this
/// subfolder leaves the provider as a pure placement layer. Not intended for
/// direct use by application code.
/// </summary>
public partial class NoticeCard : AeterniComponent
{
    [Parameter]
    public bool IsAlert { get; set; }

    [Parameter]
    public Severity Severity { get; set; } = Severity.Info;

    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public RenderFragment? Content { get; set; }

    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    [Parameter]
    public bool NoBlur { get; set; }

    [Parameter]
    public bool IsClosing { get; set; }

    [Parameter]
    public TimeSpan Duration { get; set; }

    [Parameter]
    public string? CloseLabel { get; set; }

    [Parameter]
    public EventCallback CloseRequested { get; set; }

    /// <summary>
    /// Whether the auto-dismiss countdown is currently paused. Set by the provider from
    /// the service entry; the class pauses the drain animation in step with the timer.
    /// </summary>
    [Parameter]
    public bool Paused { get; set; }

    /// <summary>
    /// Reports pointer/focus pause changes to the provider, which pauses or resumes the
    /// service-side countdown. Defaults to no-op for a card rendered outside the provider.
    /// </summary>
    [Parameter]
    public EventCallback<bool> PauseChanged { get; set; }

    private bool _pointerInside;
    private bool _focusInside;
    private bool _pauseReported;

    private void HandlePointerEnter()
    {
        _pointerInside = true;
        SyncPauseState();
    }

    private void HandlePointerLeave()
    {
        _pointerInside = false;
        SyncPauseState();
    }

    private void HandleFocusEnter()
    {
        _focusInside = true;
        SyncPauseState();
    }

    private void HandleFocusLeave()
    {
        _focusInside = false;
        SyncPauseState();
    }

    private void SyncPauseState()
    {
        var paused = _pointerInside || _focusInside;
        if (paused == _pauseReported)
        {
            return;
        }

        _pauseReported = paused;
        _ = PauseChanged.InvokeAsync(paused);
    }

    private string Role => IsAlert ? "alert" : "status";

    private string Live => IsAlert || Severity == Severity.Danger ? "assertive" : "polite";

    private string DefaultCloseLabel => IsAlert ? UiText.AlertCloseLabel : UiText.ToastCloseLabel;

    private string CardKindClass => IsAlert
        ? "aeterni-dialog-provider__alert"
        : "aeterni-dialog-provider__toast";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Severity))
        {
            throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "Unknown notice severity.");
        }
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add(CardKindClass)
            // Severity → colour lives in ComponentClass only. Severity.Default maps to
            // Color.Primary, whose `--primary` modifier shares the base accent rule.
            .Add(ComponentClass.ForColor(CardKindClass, Severity.ToColor()))
            .Add("is-no-blur", NoBlur)
            .Add("is-paused", Paused)
            .Add("is-closing", IsClosing);
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = Role,
            ["aria-live"] = Live
        };

        return attributes;
    }

    private async Task HandleCloseAsync(MouseEventArgs args)
    {
        if (CloseRequested.HasDelegate)
        {
            await CloseRequested.InvokeAsync();
        }
    }

    private static IconDefinition NoticeIcon(Severity severity) => severity switch
    {
        Severity.Success => AeterniIcons.Check,
        Severity.Warning => AeterniIcons.Exclamation,
        Severity.Danger => AeterniIcons.Xmark,
        _ => AeterniIcons.Info
    };

    private static string ProgressStyle(TimeSpan duration) =>
        $"--aeterni-dialog-duration: {duration.TotalMilliseconds.ToString("0.##", CultureInfo.InvariantCulture)}ms;";
}
