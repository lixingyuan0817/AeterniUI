using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Models.Dialog;

public sealed class ToastOptions
{
    public string? Title { get; set; }

    public Severity Severity { get; set; } = Severity.Info;

    public RenderFragment? Icon { get; set; }

    public ToastPosition? Position { get; set; }

    /// <summary>
    /// The duration of the toast. A null value uses the global service default;
    /// TimeSpan.Zero keeps the toast visible until it is closed manually.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    public bool ShowCloseButton { get; set; } = true;

    public bool Blur { get; set; } = true;

    public Func<Task>? OnClosedAsync { get; set; }
}
