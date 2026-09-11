using AeterniUI.Enums;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Theme;

/// <summary>
/// Theme mode picker (System / Light / Dark). It is a thin adapter over
/// <see cref="Segmented{TValue}"/>: the control owns the radiogroup semantics and the
/// sliding indicator, this component owns the mode list, the localised labels and the
/// ThemeService calls.
/// </summary>
public partial class ThemeSwitch : AeterniComponent
{
    [Inject]
    protected ThemeService ThemeService { get; set; } = default!;

    /// <summary>
    /// Accessible name of the mode group. Defaults to
    /// <see cref="AeterniUITextOptions.ThemeSwitchLabel"/>.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public EventCallback<ThemeMode> ModeChanged { get; set; }

    protected override void OnComponentInitialized()
    {
        ThemeService.ThemeChanged += HandleThemeChanged;
    }

    private string GroupLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.ThemeSwitchLabel : AriaLabel;

    /// <summary>
    /// Option order of the switch. The order is part of the component contract: the
    /// mode labels follow it and so does the indicator's travel.
    /// </summary>
    private static readonly ThemeMode[] Modes = [ThemeMode.System, ThemeMode.Light, ThemeMode.Dark];

    private string ModeLabel(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => UiText.ThemeLightLabel,
        ThemeMode.Dark => UiText.ThemeDarkLabel,
        _ => UiText.ThemeSystemLabel
    };

    /// <summary>
    /// Reports the control's root element as this component's own, so the base class
    /// parameters (<c>Element</c> / <c>ElementChanged</c>) describe the same element the
    /// consumer sees.
    /// </summary>
    private async Task ForwardElementChangedAsync(ElementReference element)
    {
        SetRootElement(element);

        if (ElementChanged.HasDelegate)
        {
            await ElementChanged.InvokeAsync(element);
        }
    }

    protected async Task SelectModeAsync(ThemeMode mode)
    {
        if (Disabled || ThemeService.Mode == mode)
        {
            return;
        }

        switch (mode)
        {
            case ThemeMode.System:
                ThemeService.UseSystem();
                break;
            case ThemeMode.Light:
                ThemeService.SetLight();
                break;
            case ThemeMode.Dark:
                ThemeService.SetDark();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown theme mode.");
        }

        if (ModeChanged.HasDelegate)
        {
            await ModeChanged.InvokeAsync(mode);
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        ThemeService.ThemeChanged -= HandleThemeChanged;
        return ValueTask.CompletedTask;
    }

    private void HandleThemeChanged(object? sender, EventArgs args)
    {
        if (!IsDisposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
