namespace AeterniUI.Models.Dialog;

public sealed class DialogResult
{
    private DialogResult(bool confirmed, bool cancelled, object? data)
    {
        Confirmed = confirmed;
        Cancelled = cancelled;
        Data = data;
    }

    public bool Confirmed { get; }

    public bool Cancelled { get; }

    public bool Dismissed => !Confirmed && !Cancelled;

    public object? Data { get; }

    public static DialogResult Confirm(object? data = null) => new(true, false, data);

    public static DialogResult Cancel(object? data = null) => new(false, true, data);

    public static DialogResult Dismiss(object? data = null) => new(false, false, data);
}
