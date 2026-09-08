using AeterniUI.Attributes;
using AeterniUI.Enums;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Theme;

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
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsReady = true;
            try
            {
                if (ThemeService.Mode == ThemeMode.System)
                {
                    await RefreshSystemThemeAsync();
                }

                await ApplyThemeAsync(animate: false);
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

    protected override ValueTask OnComponentDisposeAsync()
    {
        _disposed = true;
        ThemeService.ThemeChanged -= HandleThemeChanged;
        return ValueTask.CompletedTask;
    }
}
