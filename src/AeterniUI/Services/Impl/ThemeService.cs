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
