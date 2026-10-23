namespace AeterniUI.Models.Dialog;

/// <summary>
/// Options for a form dialog created through
/// <see cref="IDialogService.ShowForm" /> / <see cref="IDialogService.ShowFormAsync" />.
/// <para>
/// The footer is owned by the provider: it renders a secondary cancel action and a
/// primary submit action from <see cref="CancelText" /> / <see cref="SubmitText" />,
/// so <see cref="DialogOptions.Footer" /> is not used for the form kind. The rest of
/// the inherited chrome (title, close button, Escape and overlay behaviour) applies
/// unchanged.
/// </para>
/// </summary>
public sealed class FormDialogOptions : DialogOptions
{
    /// <summary>
    /// A form dialog is for filling in fields, so the first field is focused by
    /// default; its inherited
    /// <see cref="DialogOptions.AutofocusFirstField" /> stays available to turn the
    /// behaviour off.
    /// </summary>
    public FormDialogOptions()
    {
        AutofocusFirstField = true;
    }

    /// <summary>
    /// Label of the primary submit action. An empty or null value falls back to
    /// <see cref="AeterniUITextOptions.FormDialogSubmitLabel" />.
    /// </summary>
    public string? SubmitText { get; set; }

    /// <summary>
    /// Label of the secondary cancel action. An empty or null value falls back to
    /// <see cref="AeterniUITextOptions.FormDialogCancelLabel" />.
    /// </summary>
    public string? CancelText { get; set; }

    /// <summary>
    /// Invoked when the primary action is pressed. While it runs the dialog is busy:
    /// the submit button shows its loading state, the cancel and close actions are
    /// disabled, and Escape, overlay clicks and repeated submits are ignored. A
    /// successful result closes the dialog; a failed result writes
    /// <see cref="FormDialogSubmitResult.Message" /> to
    /// <see cref="ValidationMessage" /> and keeps it open. When left null the
    /// dialog closes with <see cref="DialogResult.Confirm" />.
    /// </summary>
    public Func<Task<FormDialogSubmitResult>>? OnSubmit { get; set; }

    /// <summary>
    /// Invoked before the secondary action closes the dialog. An exception keeps the
    /// dialog open, because the close only happens after the callback finishes.
    /// </summary>
    public Func<Task>? OnCancel { get; set; }

    /// <summary>
    /// Whether pressing Enter in a form field activates the submit action. Textareas,
    /// buttons, links and selects are exempt because Enter has its own meaning there.
    /// </summary>
    public bool SubmitOnEnter { get; set; } = true;

    /// <summary>
    /// The last validation message returned by a failed submission. The provider
    /// renders it in a <c>role="alert"</c> region; the service clears it when the next
    /// submission starts. Hosts normally leave it alone.
    /// </summary>
    public string? ValidationMessage { get; set; }
}
