using AeterniUI.Attributes;
using AeterniUI.Enums;
using AeterniUI.Models.Dialog;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Dialog;

[JsModule("Components/Dialog/DialogProvider.razor.js", Name = "dialog-provider", Interactive = true)]
public partial class DialogProvider : AeterniComponent
{
    private static readonly ToastPosition[] NoticePositions = Enum.GetValues<ToastPosition>();
    private readonly HashSet<string> _shakingDialogs = [];

    [Inject]
    protected DialogService DialogService { get; set; } = default!;

    private bool _jsReady;

    protected override void OnComponentInitialized()
    {
        DialogService.Changed += HandleDialogChanged;
    }

    protected override ClassBuilder BuildClass()
    {
        return base.BuildClass()
            .Add("aeterni-dialog-provider");
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsReady = true;
        }

        if (!_jsReady)
        {
            return;
        }

        try
        {
            var topDialogId = DialogService.GetTopDialog()?.Id;

            await JsModuleManager.InvokeModuleVoidAsync(
                "dialog-provider",
                "sync",
                RootElement,
                topDialogId,
                topDialogId is not null);

            // Re-measure the SVG border progress rings after every render.
            await JsModuleManager.InvokeModuleVoidAsync(
                "dialog-provider",
                "initProgress",
                RootElement);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
        }
    }

    private void HandleDialogChanged(object? sender, EventArgs args)
    {
        if (!IsDisposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key != "Escape")
        {
            return;
        }

        var dialog = DialogService.GetTopDialog() ?? DialogService.GetTopAlert();
        if (dialog?.Options.CloseOnEscape == true)
        {
            await dialog.Reference.CloseAsync(DialogResult.Dismiss());
        }
    }

    private async Task HandleOverlayClickAsync(DialogEntry dialog)
    {
        if (dialog.Options.CloseOnOverlayClick)
        {
            await dialog.Reference.CloseAsync(DialogResult.Dismiss());
            return;
        }

        if (dialog.Kind != DialogContentKind.Confirm || IsDisposed || !_shakingDialogs.Add(dialog.Id))
        {
            return;
        }

        await InvokeAsync(StateHasChanged);

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(320));
        }
        finally
        {
            _shakingDialogs.Remove(dialog.Id);
            if (!IsDisposed)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private Task CloseDialogAsync(DialogEntry dialog, DialogResult result) =>
        dialog.Reference.CloseAsync(result);

    private static string PositionClass(ToastPosition position) => position switch
    {
        ToastPosition.TopStart => "top-start",
        ToastPosition.TopCenter => "top-center",
        ToastPosition.TopEnd => "top-end",
        ToastPosition.CenterStart => "center-start",
        ToastPosition.CenterEnd => "center-end",
        ToastPosition.BottomStart => "bottom-start",
        ToastPosition.BottomCenter => "bottom-center",
        ToastPosition.BottomEnd => "bottom-end",
        _ => "bottom-end"
    };

    private static string SeverityClass(Severity severity) => severity switch
    {
        Severity.Info => "info",
        Severity.Success => "success",
        Severity.Warning => "warning",
        Severity.Danger => "danger",
        _ => "default"
    };

    private static Color ActionColor(Severity severity) => severity switch
    {
        Severity.Warning => Color.Warning,
        Severity.Danger => Color.Danger,
        Severity.Success => Color.Success,
        Severity.Info => Color.Info,
        _ => Color.Primary
    };

    private bool IsDialogShaking(string id) => _shakingDialogs.Contains(id);

    protected override ValueTask OnComponentDisposeAsync()
    {
        DialogService.Changed -= HandleDialogChanged;

        _shakingDialogs.Clear();
        _jsReady = false;
        return ValueTask.CompletedTask;
    }
}
