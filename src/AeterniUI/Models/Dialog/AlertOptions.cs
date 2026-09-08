using AeterniUI.Enums;

namespace AeterniUI.Models.Dialog;

public sealed class AlertOptions : DialogOptions
{
    public AlertOptions()
    {
        Severity = Severity.Info;
    }

    public string CloseText { get; set; } = "Close";

    /// <summary>
    /// The duration of the alert. A null value uses the global service default;
    /// TimeSpan.Zero keeps the alert visible until it is closed manually.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    public bool Blur { get; set; } = true;

    public Func<Task>? OnClosedAsync { get; set; }
}
