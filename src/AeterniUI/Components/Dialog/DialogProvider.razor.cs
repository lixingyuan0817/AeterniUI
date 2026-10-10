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
                InstanceId,
                RootElement,
                topDialogId,
                topDialogId is not null);

            // Re-measure the SVG border progress rings after every render.
            await JsModuleManager.InvokeModuleVoidAsync(
                "dialog-provider",
                "initProgress",
                InstanceId,
                RootElement);

            // REV-148: retiring an entry waits for its measured exit animation. The
            // previous fixed delays cut the 300ms animation off at 60%/73%, and under
            // reduced motion there is no animation left, so this returns immediately.
            var closingIds = DialogService.GetClosingIds();
            if (closingIds.Count > 0)
            {
                await JsModuleManager.InvokeModuleVoidAsync(
                    "dialog-provider",
                    "waitForExitSignals",
                    InstanceId,
                    closingIds);

                foreach (var id in closingIds)
                {
                    DialogService.SignalExit(id);
                }
            }
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
            // Wait for the shake the stylesheet is actually running rather than a
            // hand-tuned delay, which would outlive a shortened animation and would
            // still block under reduced motion where the duration collapses to zero.
            await JsModuleManager.InvokeModuleVoidAsync("dialog-provider", "waitForShake", InstanceId, dialog.Id);
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

    /// <summary>
    /// Dialog chrome classes. The severity is expressed with the shared colour
    /// mapping, so a default severity adds no modifier (the base rule already
    /// carries the brand accent).
    /// </summary>
    private string BuildDialogClass(DialogEntry dialog) => ClassBuilder("aeterni-dialog-provider__dialog")
        .Add(ComponentClass.ForColor("aeterni-dialog-provider__dialog", dialog.Options.Severity.ToColor()))
        .Add("is-shaking", IsDialogShaking(dialog.Id))
        .Add("is-closing", dialog.IsClosing)
        .Build();

    private bool IsDialogShaking(string id) => _shakingDialogs.Contains(id);

    protected override ValueTask OnComponentDisposeAsync()
    {
        DialogService.Changed -= HandleDialogChanged;

        _shakingDialogs.Clear();
        _jsReady = false;
        return ValueTask.CompletedTask;
    }
}
