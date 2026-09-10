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

    private string Role => IsAlert ? "alert" : "status";

    private string Live => IsAlert || Severity == Severity.Danger ? "assertive" : "polite";

    private string DefaultCloseLabel => IsAlert ? "Close alert" : "Close notification";

    private string CardKindClass => IsAlert
        ? "aeterni-dialog-provider__alert"
        : "aeterni-dialog-provider__toast";

    private string BuildCardClass()
    {
        var classes = new List<string>
        {
            CardKindClass,
            $"{CardKindClass}--{SeverityClass(Severity)}"
        };

        if (NoBlur)
        {
            classes.Add("is-no-blur");
        }

        if (IsClosing)
        {
            classes.Add("is-closing");
        }

        return string.Join(" ", classes);
    }

    private static string SeverityClass(Severity severity) => severity switch
    {
        Severity.Info => "info",
        Severity.Success => "success",
        Severity.Warning => "warning",
        Severity.Danger => "danger",
        _ => "default"
    };

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
