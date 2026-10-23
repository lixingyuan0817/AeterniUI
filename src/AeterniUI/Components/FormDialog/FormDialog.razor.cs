using AeterniUI.Models.Dialog;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.FormDialog;

/// <summary>
/// Declarative host for a form dialog. The component renders no DOM of its own: it
/// forwards <see cref="ChildContent" /> to
/// <see cref="IDialogService.ShowForm" />, so the modal chrome (backdrop, focus
/// trap, scroll lock, closing animation) is the same one <c>DialogProvider</c>
/// owns for every other dialog.
/// <para>
/// <see cref="Open" /> is controlled: the component opens the dialog when it turns
/// true, reports <c>false</c> through <see cref="OpenChanged" /> when the dialog
/// closes (submit, cancel, close button, Escape or overlay), and closes the dialog
/// when the host sets it back to false.
/// </para>
/// </summary>
public partial class FormDialog : AeterniComponent
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private static readonly RenderFragment EmptyContent = _ => { };

    private DialogReference? _reference;
    private bool _closeRequested;
    private bool _pendingReopen;
    private bool _disposed;

    /// <summary>
    /// Whether the dialog is open. The parameter is controlled: the component never
    /// mutates it silently, it raises <see cref="OpenChanged" /> so
    /// <c>@bind-Open</c> stays the single source of truth.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Header heading of the dialog. It also names the dialog for assistive technology.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Label of the primary action. Defaults to
    /// <see cref="AeterniUITextOptions.FormDialogSubmitLabel" />.
    /// </summary>
    [Parameter]
    public string? SubmitText { get; set; }

    /// <summary>
    /// Label of the secondary action. Defaults to
    /// <see cref="AeterniUITextOptions.FormDialogCancelLabel" />.
    /// </summary>
    [Parameter]
    public string? CancelText { get; set; }

    /// <summary>
    /// Runs when the primary action is pressed. Returning
    /// <see cref="FormDialogSubmitResult.Fail" /> keeps the dialog open and renders
    /// the message; returning <see cref="FormDialogSubmitResult.Success" /> closes
    /// it. The provider owns the busy state, so a slow handler disables cancel and
    /// close for the duration. When left null the primary action just closes the
    /// dialog.
    /// </summary>
    [Parameter]
    public Func<Task<FormDialogSubmitResult>>? OnSubmit { get; set; }

    /// <summary>Runs before the secondary action closes the dialog.</summary>
    [Parameter]
    public Func<Task>? OnCancel { get; set; }

    /// <summary>Whether the first form field receives focus when the dialog opens.</summary>
    [Parameter]
    public bool AutofocusFirstField { get; set; } = true;

    /// <summary>Whether Enter in a single-line field activates the primary action.</summary>
    [Parameter]
    public bool SubmitOnEnter { get; set; } = true;

    /// <summary>Whether Escape requests a close. A busy submission ignores it.</summary>
    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// The form body and fields. The provider renders it inside the dialog, so it
    /// takes part in the dialog's normal focus and validation flow.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (_disposed)
        {
            return;
        }

        if (!Visible || !Open)
        {
            _pendingReopen = false;
            RequestClose();
            return;
        }

        if (_reference is null)
        {
            StartOpen();
        }
        else if (_closeRequested)
        {
            // The host reopened while the previous dialog was still playing its exit
            // animation. Opening a second dialog now would stack two panels, so the
            // reopen waits for the close to finish.
            _pendingReopen = true;
        }
    }

    private void StartOpen()
    {
        var options = new FormDialogOptions
        {
            Title = Title,
            SubmitText = SubmitText,
            CancelText = CancelText,
            OnSubmit = OnSubmit,
            OnCancel = OnCancel,
            AutofocusFirstField = AutofocusFirstField,
            SubmitOnEnter = SubmitOnEnter,
            CloseOnEscape = CloseOnEscape
        };

        var reference = DialogService.ShowForm(ChildContent ?? EmptyContent, options);
        _reference = reference;
        _closeRequested = false;
        _ = TrackAsync(reference);
    }

    private async Task TrackAsync(DialogReference reference)
    {
        try
        {
            await reference.Result;
        }
        catch
        {
            // A cancelled/faulted completion means the dialog is gone; there is no
            // state left to reconcile.
            return;
        }

        if (_disposed)
        {
            return;
        }

        await InvokeAsync(() => HandleResultAsync(reference));
    }

    private async Task HandleResultAsync(DialogReference reference)
    {
        if (_disposed || !ReferenceEquals(_reference, reference))
        {
            return;
        }

        var reopen = _pendingReopen && Open && Visible;
        var closedByHost = _closeRequested;

        _reference = null;
        _closeRequested = false;
        _pendingReopen = false;

        if (reopen)
        {
            StartOpen();
        }
        else if (!closedByHost && Open)
        {
            // A submit, cancel or close action inside the dialog ended it; mirror
            // that into the controlled parameter instead of leaving Open=true with
            // no dialog on screen.
            Open = false;
            if (OpenChanged.HasDelegate)
            {
                await OpenChanged.InvokeAsync(false);
            }
        }

        if (!_disposed)
        {
            StateHasChanged();
        }
    }

    private void RequestClose()
    {
        if (_reference is null || _closeRequested)
        {
            return;
        }

        _closeRequested = true;
        _ = CloseAsync(_reference);
    }

    private static async Task CloseAsync(DialogReference reference)
    {
        try
        {
            await reference.CloseAsync(DialogResult.Dismiss());
        }
        catch
        {
            // Closing is best-effort: the service can already be disposed with the
            // circuit, and the reference then completes through that path.
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _disposed = true;
        _pendingReopen = false;
        _closeRequested = true;

        var reference = _reference;
        _reference = null;
        if (reference is not null)
        {
            _ = CloseAsync(reference);
        }

        return ValueTask.CompletedTask;
    }
}
