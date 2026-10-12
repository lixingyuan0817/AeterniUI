using AeterniUI.Enums;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Models;

/// <summary>
/// One file the user selected, plus the transfer state the host reports back.
/// </summary>
/// <remarks>
/// The component creates these instances and keeps them stable across
/// re-selections, so the host can mutate <see cref="Status"/>, <see cref="Percent"/>
/// and <see cref="ErrorMessage"/> on the instance it received — a re-render will
/// not replace the object under the host. Instances cannot be constructed from
/// outside the library: a row that carries no <see cref="Content"/> would be a
/// row the host can never transfer, and pre-existing server-side files belong in
/// the host's own markup rather than in this list.
/// </remarks>
public sealed class UploadFile
{
    internal UploadFile(IBrowserFile content)
    {
        Content = content;
        Id = $"aeterni-upload-file-{Guid.NewGuid():N}";
    }

    /// <summary>The selected file, for reading its stream when the host transfers it.</summary>
    public IBrowserFile Content { get; }

    /// <summary>Stable identity of this row, used for keying and accessible names.</summary>
    public string Id { get; }

    /// <summary>File name as the browser reported it.</summary>
    public string Name => Content.Name;

    /// <summary>File size in bytes as the browser reported it.</summary>
    public long Size => Content.Size;

    /// <summary>Media type as the browser reported it; may be empty.</summary>
    public string ContentType => Content.ContentType;

    /// <summary>
    /// Transfer state. The component sets it to <see cref="UploadStatus.Error"/>
    /// when it rejects a file against its limits; every other transition belongs
    /// to the host.
    /// </summary>
    public UploadStatus Status { get; set; } = UploadStatus.Pending;

    /// <summary>
    /// Transfer completion in percent, rendered while <see cref="Status"/> is
    /// <see cref="UploadStatus.Uploading"/>. Values outside 0..100 are clamped
    /// when rendered.
    /// </summary>
    public double? Percent { get; set; }

    /// <summary>
    /// Reason shown next to the file when <see cref="Status"/> is
    /// <see cref="UploadStatus.Error"/>. The component fills it for its own
    /// rejections; the host fills it for transfer failures.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
