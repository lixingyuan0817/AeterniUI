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

    /// <summary>
    /// Opens a modal form dialog: the provider renders the form body plus a footer
    /// with a cancel and a submit action, drives the busy state around
    /// <see cref="FormDialogOptions.OnSubmit" /> and shows its validation message.
    /// </summary>
    DialogReference ShowForm(RenderFragment content, FormDialogOptions? options = null);

    /// <summary>
    /// Opens a form dialog and returns its completion: confirmed with
    /// <see cref="DialogResult.Confirm(object?)" /> on a successful submission,
    /// cancelled when the cancel action was used, dismissed for every other close.
    /// </summary>
    Task<DialogResult> ShowFormAsync(RenderFragment content, FormDialogOptions? options = null);

    Task<DialogResult> AlertAsync(string message, AlertOptions? options = null);

    Task<DialogResult> AlertAsync(string message, ToastPosition position, AlertOptions? options = null);

    Task<bool> ConfirmAsync(string message, ConfirmOptions? options = null);

    ToastReference ShowToast(string message, ToastOptions? options = null);

    ToastReference ShowToast(string message, ToastPosition position, ToastOptions? options = null);

    ToastReference ShowToast(RenderFragment content, ToastOptions? options = null);

    ToastReference ShowToast(RenderFragment content, ToastPosition position, ToastOptions? options = null);
}
