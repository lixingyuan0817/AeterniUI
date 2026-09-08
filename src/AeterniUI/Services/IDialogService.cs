using AeterniUI.Enums;
using AeterniUI.Models.Dialog;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Services;

public interface IDialogService
{
    ToastPosition DefaultToastPosition { get; set; }

    ToastPosition DefaultAlertPosition { get; set; }

    int MaxToastCount { get; set; }

    TimeSpan DefaultAlertDuration { get; set; }

    TimeSpan DefaultToastDuration { get; set; }

    DialogReference Show(RenderFragment content, DialogOptions? options = null);

    Task<DialogResult> ShowAsync(RenderFragment content, DialogOptions? options = null);

    Task<DialogResult> AlertAsync(string message, AlertOptions? options = null);

    Task<DialogResult> AlertAsync(string message, ToastPosition position, AlertOptions? options = null);

    Task<bool> ConfirmAsync(string message, ConfirmOptions? options = null);

    ToastReference ShowToast(string message, ToastOptions? options = null);

    ToastReference ShowToast(string message, ToastPosition position, ToastOptions? options = null);

    ToastReference ShowToast(RenderFragment content, ToastOptions? options = null);

    ToastReference ShowToast(RenderFragment content, ToastPosition position, ToastOptions? options = null);
}
