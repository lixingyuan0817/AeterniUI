namespace AeterniUI.Models.Dialog;

/// <summary>
/// Outcome of a form dialog submission, returned from
/// <see cref="FormDialogOptions.OnSubmit" />.
/// <para>
/// A failed result keeps the dialog open and surfaces <see cref="Message" /> in the
/// provider-rendered validation area (<c>role="alert"</c>), so the user can correct
/// the form without losing its state. A successful result closes the dialog and
/// exposes <see cref="Data" /> on <see cref="DialogResult.Data" />.
/// </para>
/// </summary>
public sealed class FormDialogSubmitResult
{
    private FormDialogSubmitResult(bool succeeded, string? message, object? data)
    {
        Succeeded = succeeded;
        Message = message;
        Data = data;
    }

    /// <summary>Whether the submission succeeded and the dialog should close.</summary>
    public bool Succeeded { get; }

    /// <summary>Validation message rendered while the dialog stays open.</summary>
    public string? Message { get; }

    /// <summary>Result payload exposed to the caller when the submission succeeds.</summary>
    public object? Data { get; }

    /// <summary>Creates a successful result, optionally carrying a payload.</summary>
    public static FormDialogSubmitResult Success(object? data = null) => new(true, null, data);

    /// <summary>
    /// Creates a failed result whose message is rendered in the dialog's validation
    /// area. The message may not be empty: a failure without feedback would look
    /// like the submit button did nothing.
    /// </summary>
    public static FormDialogSubmitResult Fail(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, message.Trim(), null);
    }
}
