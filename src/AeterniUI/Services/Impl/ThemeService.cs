using AeterniUI.Enums;

namespace AeterniUI.Services.Impl;

public sealed class ThemeService
{
    public ThemeMode Mode { get; private set; } = ThemeMode.System;

    public ThemeKind CurrentTheme { get; private set; } = ThemeKind.Light;

    public bool IsDark => CurrentTheme == ThemeKind.Dark;

    public event EventHandler? ThemeChanged;

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
        if (mode != ThemeMode.System)
        {
            SetCurrentTheme(mode == ThemeMode.Dark ? ThemeKind.Dark : ThemeKind.Light);
            return;
        }

        RaiseThemeChanged();
    }

    public void UseSystem() => SetMode(ThemeMode.System);

    public void SetLight() => SetMode(ThemeMode.Light);

    public void SetDark() => SetMode(ThemeMode.Dark);

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
