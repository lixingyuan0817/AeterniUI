using AeterniUI.Models;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// A file the browser would have reported. <see cref="IBrowserFile" /> is public
/// but <c>InputFileChangeEventArgs</c> is not constructible from outside the
/// framework, so the limit pipeline is driven through the component's selection
/// entry point with these instead.
/// </summary>
internal sealed class StubBrowserFile(string name, long size) : IBrowserFile
{
    public string Name { get; } = name;

    public long Size { get; } = size;

    public string ContentType { get; } = "application/octet-stream";

    public DateTimeOffset LastModified { get; } = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The contract checks never read file content.");
}

/// <summary>
/// Receiver for the callbacks a rendered <c>Upload</c> raises. An
/// <c>EventCallback</c> needs a receiver, and the checks live in top-level
/// statements where there is no component to hand over.
/// </summary>
internal sealed class UploadCallbackReceiver
{
    public List<IReadOnlyList<UploadFile>> Selections { get; } = [];

    public List<string> SelectedNames { get; } = [];
}
