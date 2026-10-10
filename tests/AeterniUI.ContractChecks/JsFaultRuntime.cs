using Microsoft.JSInterop;

/// <summary>
/// JS runtime whose module import succeeds while every module call throws
/// <see cref="JSException" />, so the contract checks can assert that a broken or
/// throwing module degrades the component instead of surfacing as an unhandled
/// render exception (REV-153).
/// </summary>
internal sealed class JsFaultRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        identifier == "import"
            ? ValueTask.FromResult((TValue)(object)new FaultingModule())
            : ValueTask.FromResult(default(TValue)!);

    private sealed class FaultingModule : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new JSException($"the module failed on '{identifier}'");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new JSException($"the module failed on '{identifier}'");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
