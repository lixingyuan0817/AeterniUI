using AeterniUI.Attributes;
using AeterniUI.Enums;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Theme;

/// <summary>
/// Theme owner. It renders <em>no DOM</em>: the component applies the theme to
/// the document root element through its JS module, so the inherited
/// <c>Id</c>, <c>Class</c>, <c>Style</c> and <c>Visible</c> parameters have no
/// effect and are intentionally unused. Place it once in the app layout.
/// </summary>
[JsModule("Components/Theme/ThemeProvider.razor.js", Name = "theme-provider", Interactive = true)]
public partial class ThemeProvider : AeterniComponent
{
    [Inject] private ThemeService ThemeService { get; set; } = default!;

    private bool _jsReady;
    private bool _disposed;
    private readonly SemaphoreSlim _themeSyncLock = new(1, 1);

    protected override void OnComponentInitialized()
    {
        ThemeService.ThemeChanged += HandleThemeChanged;
        ThemeService.BrandChanged += HandleBrandChanged;
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsReady = true;
            try
            {
                await RestoreStoredModeAsync();

                if (ThemeService.Mode == ThemeMode.System)
                {
                    await RefreshSystemThemeAsync();
                }

                await ApplyThemeAsync(animate: false);

                await RestoreStoredBrandAsync();
                await ApplyBrandAsync(animate: false);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException or JSException)
            {
            }
        }
    }

    [JSInvokable("OnSystemThemeChanged")]
    public async Task OnSystemThemeChanged(bool isDark)
    {
        if (_disposed || ThemeService.Mode != ThemeMode.System)
        {
            return;
        }

        ThemeService.ApplySystemTheme(isDark);

        if (_jsReady)
        {
            await InvokeAsync(() => ApplyThemeAsync());
        }
    }

    private void HandleThemeChanged(object? sender, EventArgs args)
    {
        _ = InvokeAsync(HandleThemeChangedAsync);
    }

    private void HandleBrandChanged(object? sender, EventArgs args)
    {
        _ = InvokeAsync(HandleBrandChangedAsync);
    }

    private async Task HandleBrandChangedAsync()
    {
        if (_disposed || !_jsReady)
        {
            return;
        }

        // Shares the theme sync lock: a brand switch and a mode switch both reach
        // the same attributes, so they must not interleave.
        await _themeSyncLock.WaitAsync();
        try
        {
            await ApplyBrandAsync();
            await PersistBrandAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException or JSException)
        {
        }
        finally
        {
            _themeSyncLock.Release();
        }
    }

    private async Task HandleThemeChangedAsync()
    {
        if (_disposed || !_jsReady)
        {
            return;
        }

        await _themeSyncLock.WaitAsync();
        try
        {
            if (ThemeService.Mode == ThemeMode.System)
            {
                await RefreshSystemThemeAsync();
            }

            await ApplyThemeAsync();
            await PersistModeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException or JSException)
        {
        }
        finally
        {
            _themeSyncLock.Release();
        }
    }

    private async Task RefreshSystemThemeAsync()
    {
        var isDark = await JsModuleManager.InvokeModuleAsync<bool?>(
            "theme-provider",
            "getSystemTheme");

        if (isDark.HasValue)
        {
            ThemeService.ApplySystemTheme(isDark.Value);
        }
    }

    /// <summary>
    /// Restores a previously stored theme mode (localStorage works both in a
    /// regular browser and inside the Tauri webview, where the storage is
    /// persisted for the app identifier). Invalid/absent values fall back to
    /// the configured default, i.e. System.
    /// </summary>
    private async Task RestoreStoredModeAsync()
    {
        var stored = await JsModuleManager.InvokeModuleAsync<string?>(
            "theme-provider",
            "getStoredMode");

        var mode = (stored?.ToLowerInvariant()) switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            "system" => ThemeMode.System,
            _ => (ThemeMode?)null
        };

        if (mode.HasValue && mode.Value != ThemeService.Mode)
        {
            ThemeService.SetMode(mode.Value);
        }
    }

    private async Task PersistModeAsync()
    {
        if (!_jsReady)
        {
            return;
        }

        try
        {
            await JsModuleManager.InvokeModuleVoidAsync(
                "theme-provider",
                "persistMode",
                ThemeService.Mode.ToString().ToLowerInvariant());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException or JSException)
        {
        }
    }

    private Task ApplyThemeAsync(bool animate = true)
    {
        if (!_jsReady)
        {
            return Task.CompletedTask;
        }

        return JsModuleManager.InvokeModuleVoidAsync(
            "theme-provider",
            "applyTheme",
            ThemeService.CurrentTheme.ToString().ToLowerInvariant(),
            animate,
            ThemeService.Mode.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Restores a previously stored brand. Absent or invalid values keep the
    /// configured default, so a host that never opts into the brand axis is
    /// unaffected.
    /// </summary>
    private async Task RestoreStoredBrandAsync()
    {
        var stored = await JsModuleManager.InvokeModuleAsync<string?>(
            "theme-provider",
            "getStoredBrand");

        var brand = (stored?.ToLowerInvariant()) switch
        {
            "purple" => ThemeBrand.Purple,
            "green" => ThemeBrand.Green,
            _ => (ThemeBrand?)null
        };

        if (brand.HasValue && brand.Value != ThemeService.Brand)
        {
            ThemeService.SetBrand(brand.Value);
        }
    }

    private Task ApplyBrandAsync(bool animate = true)
    {
        if (!_jsReady)
        {
            return Task.CompletedTask;
        }

        return JsModuleManager.InvokeModuleVoidAsync(
            "theme-provider",
            "applyBrand",
            ThemeService.Brand.ToString().ToLowerInvariant(),
            animate);
    }

    private async Task PersistBrandAsync()
    {
        if (!_jsReady)
        {
            return;
        }

        try
        {
            await JsModuleManager.InvokeModuleVoidAsync(
                "theme-provider",
                "persistBrand",
                ThemeService.Brand.ToString().ToLowerInvariant());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException or JSException)
        {
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _disposed = true;
        ThemeService.ThemeChanged -= HandleThemeChanged;
        ThemeService.BrandChanged -= HandleBrandChanged;
        return ValueTask.CompletedTask;
    }
}
