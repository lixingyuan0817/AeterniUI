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
/// <param name="OnGlassSecondary">Glass-only secondary ink, <c>--aeterni-text-on-glass-secondary</c>.</param>
/// <param name="OnGlassTertiary">Glass-only tertiary ink, <c>--aeterni-text-on-glass-tertiary</c>.</param>
internal sealed record GlassTheme(
    string Label,
    GlassRgba Fill,
    GlassRgba WorstCase,
    GlassRgba Primary,
    GlassRgba Secondary,
    GlassRgba Tertiary,
    GlassRgba OnGlassSecondary,
    GlassRgba OnGlassTertiary)
{
    /// <summary>
    /// Fraction of the body ink's alpha that the glass-only secondary ink keeps.
    /// </summary>
    /// <remarks>
    /// 0.93 is what makes the role derivable rather than hand-tuned per theme. The CSS
    /// declares it as <c>color-mix(in srgb, var(--aeterni-text) 93%, transparent)</c>, so the
    /// only input is the body ink the theme already publishes: light lands on
    /// <c>rgba(0,0,0,.7254)</c> and dark on <c>rgba(255,255,255,.7998)</c>. Picking explicit
    /// per-theme rgba values instead would need the three dark selectors repeated in the sample
    /// sheet and would be free to drift away from these numbers.
    /// </remarks>
    public const double OnGlassInkFraction = 0.93;

    /// <summary>
    /// Fraction of the body ink's alpha that the glass-only tertiary ink keeps.
    /// </summary>
    /// <remarks>
    /// 0.67 mirrors the library's <c>color-mix(in srgb, var(--aeterni-text) 67%, transparent)</c>.
    /// The tertiary role only carries icons and decoration on glass, so it answers to the 3:1
    /// graphic floor instead of 4.5:1 - but it is the role that sets the light theme's fill floor,
    /// because 67% of the body ink is the weakest ink a pane carries and it is measured against
    /// pure black. At the adopted 68% fill it clears that floor with 1.15x to spare; the dark theme
    /// is bound by <see cref="OnGlassInkFraction"/> instead, at 1.14x.
    /// </remarks>
    public const double OnGlassTertiaryFraction = 0.67;
}

/// <summary>
/// Theme values the glass comparison page measures against.
/// </summary>
/// <remarks>
/// Every ink value is copied from <c>aeterni_ui.css</c>, so the readouts describe the library as it
/// stands rather than an invented palette. The two fills are the adopted ones: light
/// <c>rgba(255,255,255,.68)</c> and dark <c>rgba(26,26,29,.72)</c>, against the raised fill
/// (<c>--aeterni-bg-elevated</c>) alpha of <c>.82</c> that they used to inherit. Lowering them
/// only became possible once the frosted surfaces stopped borrowing the standard ink ladder, and the
/// two limits are not the same number scaled: the standard secondary ink clears 4.5:1 down to
/// <c>.679</c> light and <c>.834</c> dark, so light is pinned by the ink it just replaced while dark
/// would need to stay almost opaque. The glass-only inks take over from there - 4.5:1 holds down to
/// <c>.551</c> light / <c>.679</c> dark for the secondary ink and 3:1 down to <c>.561</c> light /
/// <c>.656</c> dark for the tertiary one - and what the adopted fills are actually spent on is the
/// project's 1.1x accounting margin, which puts the light floor at <c>.637</c> (tertiary, 1.15x) and
/// the dark floor at <c>.710</c> (secondary, 1.14x). Both inks are derived from the body ink by
/// <see cref="GlassTheme.OnGlassInkFraction"/> and <see cref="GlassTheme.OnGlassTertiaryFraction"/>
/// rather than written per theme, so both themes keep the same margin and the single CSS declaration
/// per ink cannot drift from these numbers.
/// </remarks>
internal static class GlassPalette
{
    /// <summary>Light theme: white fill over an assumed black backdrop.</summary>
    public static GlassTheme Light { get; } = Create(
        "浅色",
        new GlassRgba(255, 255, 255, 0.68),
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
        primary.AtAlpha(primary.A * GlassTheme.OnGlassInkFraction),
        primary.AtAlpha(primary.A * GlassTheme.OnGlassTertiaryFraction));
}

/// <summary>Contrast of each ink role against one glass fill over one worst-case backdrop.</summary>
/// <param name="Primary">Body ink against the composited glass.</param>
/// <param name="Secondary">Secondary ink against the composited glass.</param>
/// <param name="OnGlassSecondary">Glass-only secondary ink.</param>
/// <param name="Tertiary">Tertiary ink against the composited glass.</param>
/// <param name="OnGlassTertiary">Glass-only tertiary ink.</param>
internal readonly record struct GlassReadout(
    double Primary,
    double Secondary,
    double OnGlassSecondary,
    double Tertiary,
    double OnGlassTertiary);

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
            GlassContrast.Ratio(theme.Tertiary, glass),
            GlassContrast.Ratio(theme.OnGlassTertiary, glass));
    }
}
