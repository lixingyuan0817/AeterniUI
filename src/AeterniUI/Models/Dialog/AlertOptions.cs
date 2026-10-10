using AeterniUI.Enums;

namespace AeterniUI.Models.Dialog;

public sealed class AlertOptions : DialogOptions
{
    public AlertOptions()
    {
        Severity = Severity.Info;
    }

    /// <summary>
    /// Accessible name of the alert's close button. An empty or null value falls
    /// back to <see cref="AeterniUITextOptions.AlertCloseLabel" />, so a host that
    /// localises through the text table keeps working without setting this.
    /// </summary>
    public string? CloseText { get; set; }

    /// <summary>
    /// The placement of the alert. A null value uses the service default
    /// (configured through <see cref="IDialogService.DefaultAlertPosition"/>).
    /// </summary>
    public ToastPosition? Position { get; set; }

    /// <summary>
    /// The duration of the alert. A null value uses the global service default;
    /// TimeSpan.Zero keeps the alert visible until it is closed manually.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    public bool Blur { get; set; } = true;

    public Func<Task>? OnClosedAsync { get; set; }
}
