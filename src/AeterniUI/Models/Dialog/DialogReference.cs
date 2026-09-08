namespace AeterniUI.Models.Dialog;

public sealed class DialogReference
{
    private readonly Func<DialogResult, Task> _close;
    private readonly TaskCompletionSource<DialogResult> _completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    internal DialogReference(string id, Func<DialogResult, Task> close)
    {
        Id = id;
        _close = close;
    }

    public string Id { get; }

    public Task<DialogResult> Result => _completion.Task;

    public Task CloseAsync(DialogResult? result = null) =>
        _close(result ?? DialogResult.Dismiss());

    internal bool TryComplete(DialogResult result) => _completion.TrySetResult(result);
}
