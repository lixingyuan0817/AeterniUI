using AeterniUI.Enums;

namespace AeterniUI.Services.Impl;

public sealed class ThemeService
{
    public ThemeService(AeterniUIOptions options)
    {
        Brand = options.DefaultBrand;
    }

    public ThemeMode Mode { get; private set; } = ThemeMode.System;

    public ThemeKind CurrentTheme { get; private set; } = ThemeKind.Light;

    /// <summary>
    /// Active brand hue layer. Independent of <see cref="Mode"/>: the brand picks
    /// the palette, the mode picks which stops of it are in effect. Starts on
    /// <see cref="AeterniUIOptions.DefaultBrand"/>.
    /// </summary>
    public ThemeBrand Brand { get; private set; }

    public bool IsDark => CurrentTheme == ThemeKind.Dark;

    public event EventHandler? ThemeChanged;

    /// <summary>
    /// Raised when <see cref="Brand"/> changes. Kept apart from
    /// <see cref="ThemeChanged"/> because a brand switch does not move the mode
    /// or the resolved light/dark theme, so it must not re-resolve the system
    /// preference.
    /// </summary>
    public event EventHandler? BrandChanged;

    public void SetMode(ThemeMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown theme mode.");
        }

        if (Mode == mode)
        {
            return;
        }

        Mode = mode;

        // Explicit Light/Dark modes always pin the effective theme. System mode
        // keeps the current value until the OS preference is re-resolved.
        if (mode != ThemeMode.System)
        {
            var next = mode == ThemeMode.Dark ? ThemeKind.Dark : ThemeKind.Light;
            if (CurrentTheme != next)
            {
                CurrentTheme = next;
            }
        }

        // Broadcast on every mode change — even when the resolved theme colour
        // did not change (e.g. picking Light while the system is already light).
        // Consumers rely on this event to persist the mode and re-apply state.
        RaiseThemeChanged();
    }

    public void UseSystem() => SetMode(ThemeMode.System);

    public void SetLight() => SetMode(ThemeMode.Light);

    public void SetDark() => SetMode(ThemeMode.Dark);

    /// <summary>
    /// Raises <see cref="BrandChanged"/> when the value actually changes.
    /// </summary>
    public void SetBrand(ThemeBrand brand)
    {
        if (!Enum.IsDefined(brand))
        {
            throw new ArgumentOutOfRangeException(nameof(brand), brand, "Unknown theme brand.");
        }

        if (Brand == brand)
        {
            return;
        }

        Brand = brand;
        BrandChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetPurple() => SetBrand(ThemeBrand.Purple);

    public void SetGreen() => SetBrand(ThemeBrand.Green);

    public void SetOrange() => SetBrand(ThemeBrand.Orange);

    internal void ApplySystemTheme(bool isDark)
    {
        if (Mode != ThemeMode.System)
        {
            return;
        }

        SetCurrentTheme(isDark ? ThemeKind.Dark : ThemeKind.Light);
    }

    private void SetCurrentTheme(ThemeKind theme)
    {
        if (CurrentTheme == theme)
        {
            return;
        }

        CurrentTheme = theme;
        RaiseThemeChanged();
    }

    private void RaiseThemeChanged() => ThemeChanged?.Invoke(this, EventArgs.Empty);
}
