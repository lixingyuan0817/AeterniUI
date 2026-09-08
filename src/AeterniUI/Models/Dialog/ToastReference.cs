namespace AeterniUI.Models.Dialog;

public sealed class ToastReference
{
    private readonly Func<Task> _close;

    internal ToastReference(string id, Func<Task> close)
    {
        Id = id;
        _close = close;
    }

    public string Id { get; }

    public Task CloseAsync() => _close();
}
