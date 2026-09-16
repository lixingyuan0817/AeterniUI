using System.Globalization;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// An sRGB colour: channels in 0-255, <see cref="A"/> in 0-1.
/// </summary>
internal readonly record struct GlassRgba(double R, double G, double B, double A)
{
    /// <summary>Pure black - the worst-case backdrop behind a light-theme glass fill.</summary>
    public static GlassRgba Black { get; } = new(0, 0, 0, 1);

    /// <summary>Pure white - the worst-case backdrop behind a dark-theme glass fill.</summary>
    public static GlassRgba White { get; } = new(255, 255, 255, 1);

    /// <summary>The same colour at another alpha.</summary>
    public GlassRgba AtAlpha(double alpha) => this with { A = alpha };
}

/// <summary>
/// WCAG 2.1 maths for the glass comparison page (<c>/components/glass</c>).
/// </summary>
/// <remarks>
/// <para>
/// The page prints numbers the contrast gate cannot produce yet. <c>scripts/check-contrast.mjs</c>
/// resolves a token pair by compositing the <em>foreground</em> and then reading the luminance of
/// the context background as if it were opaque, so handing it a translucent glass fill measures the
/// fill alone and reports a comfortable pass that no user sees. Compositing the fill is the whole
/// point: the ink lands on glass over a backdrop, and both the fill and every ink role in this
/// library already carry alpha.
/// </para>
/// <para>
/// The backdrop that matters is the worst one, not the average one. A floating panel is positioned
/// against the page and cannot choose what scrolls behind it, so the numbers here composite onto
/// pure black (light fill) and pure white (dark fill). Any real wallpaper is kinder than these.
/// </para>
/// <para>
/// <see cref="Channel"/>, <see cref="Luminance"/>, <see cref="Composite"/> and <see cref="Ratio"/>
/// deliberately mirror the script's same-named helpers so the page and the future gate agree; if
/// the script is ever made alpha-aware, these four are the functions to keep in step.
/// </para>
/// </remarks>
internal static class GlassContrast
{
    /// <summary>WCAG 2.1 floor for body text.</summary>
    public const double TextFloor = 4.5;

    /// <summary>WCAG 2.1 floor for large text, graphics and UI components.</summary>
    public const double GraphicFloor = 3.0;

    private const double GammaThreshold = 0.03928;
    private const double GammaDivisor = 12.92;
    private const double GammaOffset = 0.055;
    private const double GammaExponent = 2.4;

    /// <summary>Linearises one 0-1 sRGB channel.</summary>
    public static double Channel(double value) => value <= GammaThreshold
        ? value / GammaDivisor
        : Math.Pow((value + GammaOffset) / (1 + GammaOffset), GammaExponent);

    /// <summary>Relative luminance of a colour, ignoring alpha.</summary>
    public static double Luminance(GlassRgba colour) =>
        (0.2126 * Channel(colour.R / 255))
        + (0.7152 * Channel(colour.G / 255))
        + (0.0722 * Channel(colour.B / 255));

    /// <summary>Paints <paramref name="foreground"/> over an opaque <paramref name="background"/>.</summary>
    public static GlassRgba Composite(GlassRgba foreground, GlassRgba background)
    {
        var alpha = foreground.A + (background.A * (1 - foreground.A));
        if (alpha <= 0)
        {
            return new GlassRgba(0, 0, 0, 0);
        }

        return new GlassRgba(
            ((foreground.R * foreground.A) + (background.R * background.A * (1 - foreground.A))) / alpha,
            ((foreground.G * foreground.A) + (background.G * background.A * (1 - foreground.A))) / alpha,
            ((foreground.B * foreground.A) + (background.B * background.A * (1 - foreground.A))) / alpha,
            alpha);
    }

    /// <summary>Contrast ratio between a translucent ink and a fully composited backdrop.</summary>
    public static double Ratio(GlassRgba foreground, GlassRgba opaqueBackground)
    {
        var composited = Composite(foreground, opaqueBackground);
        var foregroundLuminance = Luminance(composited);
        var backgroundLuminance = Luminance(opaqueBackground);
        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>Formats a ratio the way the page prints it.</summary>
    public static string Format(double ratio) => ratio.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Formats a fill alpha for the matrix, for example <c>0.60</c>.</summary>
    public static string AlphaLabel(double alpha) =>
        alpha.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>
/// One theme's glass fill RGB plus the ink roles the library pairs with it.
/// </summary>
/// <param name="Label">Theme name as shown on the page.</param>
/// <param name="Fill">Glass fill; its RGB is used and <see cref="GlassRgba.A"/> is taken from the candidate.</param>
/// <param name="WorstCase">Opaque backdrop that makes this theme's glass hardest to read.</param>
/// <param name="Primary">Body ink - light <c>--aeterni-text</c>, dark its dark counterpart.</param>
/// <param name="Secondary">Secondary ink that the library uses on opaque surfaces today.</param>
/// <param name="Tertiary">Tertiary ink, carried for the 3:1 graphic floor.</param>
/// <param name="OnGlassSecondary">Proposed <c>--aeterni-text-on-glass-secondary</c>.</param>
internal sealed record GlassTheme(
    string Label,
    GlassRgba Fill,
    GlassRgba WorstCase,
    GlassRgba Primary,
    GlassRgba Secondary,
    GlassRgba Tertiary,
    GlassRgba OnGlassSecondary)
{
    /// <summary>
    /// Fraction of the body ink's alpha that the proposed on-glass secondary ink keeps.
    /// </summary>
    /// <remarks>
    /// 0.93 is what makes the new role derivable rather than hand-tuned per theme. The CSS
    /// declares it as <c>color-mix(in srgb, var(--aeterni-text) 93%, transparent)</c>, so the
    /// only input is the body ink the theme already publishes: light lands on
    /// <c>rgba(0,0,0,.7254)</c> and dark on <c>rgba(255,255,255,.7998)</c>. Picking explicit
    /// per-theme rgba values instead would need the three dark selectors repeated in the sample
    /// sheet and would be free to drift away from these numbers.
    /// </remarks>
    public const double OnGlassInkFraction = 0.93;
}

/// <summary>
/// Theme values the glass comparison page measures against.
/// </summary>
/// <remarks>
/// Every ink value is copied from <c>aeterni_ui.css</c>, so the readouts describe the library as it
/// stands rather than an invented palette. The two fills are proposals: light
/// <c>rgba(255,255,255,.60)</c> and dark <c>rgba(26,26,29,.72)</c> against today's
/// <c>--aeterni-bg-elevated</c> alpha of <c>.82</c>. <c>OnGlassSecondary</c> is decision A of the
/// glass proposal: the existing secondary ink cannot clear 4.5:1 once the fill drops below roughly
/// <c>.68</c> light / <c>.83</c> dark - and <c>.83</c> is effectively no glass at all - so the glass
/// surface needs a second secondary ink of its own. It is derived from the body ink by
/// <see cref="GlassTheme.OnGlassInkFraction"/> rather than written per theme, so both themes keep
/// the same margin and the single CSS declaration cannot drift from these numbers.
/// </remarks>
internal static class GlassPalette
{
    /// <summary>Light theme: white fill over an assumed black backdrop.</summary>
    public static GlassTheme Light { get; } = Create(
        "浅色",
        new GlassRgba(255, 255, 255, 0.60),
        GlassRgba.Black,
        new GlassRgba(0, 0, 0, 0.78),
        new GlassRgba(0, 0, 0, 0.62),
        new GlassRgba(0, 0, 0, 0.52));

    /// <summary>Dark theme: the dimmed surface fill over an assumed white backdrop.</summary>
    public static GlassTheme Dark { get; } = Create(
        "深色",
        new GlassRgba(26, 26, 29, 0.72),
        GlassRgba.White,
        new GlassRgba(255, 255, 255, 0.86),
        new GlassRgba(255, 255, 255, 0.56),
        new GlassRgba(255, 255, 255, 0.40));

    private static GlassTheme Create(
        string label,
        GlassRgba fill,
        GlassRgba worstCase,
        GlassRgba primary,
        GlassRgba secondary,
        GlassRgba tertiary) => new(
        label,
        fill,
        worstCase,
        primary,
        secondary,
        tertiary,
        primary.AtAlpha(primary.A * GlassTheme.OnGlassInkFraction));
}

/// <summary>Contrast of each ink role against one glass fill over one worst-case backdrop.</summary>
/// <param name="Primary">Body ink against the composited glass.</param>
/// <param name="Secondary">Secondary ink against the composited glass.</param>
/// <param name="OnGlassSecondary">Proposed on-glass secondary ink.</param>
/// <param name="Tertiary">Tertiary ink against the composited glass.</param>
internal readonly record struct GlassReadout(
    double Primary,
    double Secondary,
    double OnGlassSecondary,
    double Tertiary);

/// <summary>Turns a fill alpha into the readouts the page prints.</summary>
internal static class GlassMeasurement
{
    /// <summary>Measures every ink role of <paramref name="theme"/> on its fill at <paramref name="alpha"/>.</summary>
    public static GlassReadout Measure(GlassTheme theme, double alpha)
    {
        var glass = GlassContrast.Composite(theme.Fill.AtAlpha(alpha), theme.WorstCase);
        return new GlassReadout(
            GlassContrast.Ratio(theme.Primary, glass),
            GlassContrast.Ratio(theme.Secondary, glass),
            GlassContrast.Ratio(theme.OnGlassSecondary, glass),
            GlassContrast.Ratio(theme.Tertiary, glass));
    }
}
