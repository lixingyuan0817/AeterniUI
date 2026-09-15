using AeterniUI.Enums;

namespace AeterniUI.Services;

public sealed class AeterniUIOptions
{
    public ToastPosition DefaultToastPosition { get; set; } = ToastPosition.TopEnd;

    public ToastPosition DefaultAlertPosition { get; set; } = ToastPosition.BottomCenter;

    /// <summary>
    /// Brand hue layer the app starts on. A stored preference from a previous
    /// visit wins over this value, so it only sets the first-run default.
    /// <para>
    /// If the host pre-paints the theme in <c>index.html</c> (the recommended
    /// anti-flash snippet), keep that snippet's fallback brand in sync with this
    /// value — the provider applies this default after the first render, so a
    /// mismatch shows up as a one-frame colour flash.
    /// </para>
    /// </summary>
    public ThemeBrand DefaultBrand { get; set; } = ThemeBrand.Purple;

    public int MaxToastCount { get; set; } = 5;

    public TimeSpan DefaultAlertDuration { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan DefaultToastDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Text the library renders itself. Mutate the entries to localise every
    /// component at once.
    /// </summary>
    public AeterniUITextOptions Text { get; } = new();
}
