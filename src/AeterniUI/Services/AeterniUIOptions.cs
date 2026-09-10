using AeterniUI.Enums;

namespace AeterniUI.Services;

public sealed class AeterniUIOptions
{
    public ToastPosition DefaultToastPosition { get; set; } = ToastPosition.TopEnd;

    public ToastPosition DefaultAlertPosition { get; set; } = ToastPosition.BottomCenter;

    public int MaxToastCount { get; set; } = 5;

    public TimeSpan DefaultAlertDuration { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan DefaultToastDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Text the library renders itself. Mutate the entries to localise every
    /// component at once.
    /// </summary>
    public AeterniUITextOptions Text { get; } = new();
}
