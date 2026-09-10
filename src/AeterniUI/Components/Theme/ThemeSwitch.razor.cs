using AeterniUI.Enums;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Theme;

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

    private string SystemLabel => UiText.ThemeSystemLabel;

    private string LightLabel => UiText.ThemeLightLabel;

    private string DarkLabel => UiText.ThemeDarkLabel;

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-theme-switch")
            .Add(SizeClass)
            .Add("is-disabled", Disabled);
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

    private string? SizeClass => ComponentClass.ForSize("aeterni-theme-switch", Size);

    private void HandleThemeChanged(object? sender, EventArgs args)
    {
        if (!IsDisposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
