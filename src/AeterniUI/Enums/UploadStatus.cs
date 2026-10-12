namespace AeterniUI.Enums;

/// <summary>
/// Transfer state of one file in an <c>Upload</c> list. The component only
/// renders it; the host owns the transition, because it owns the transfer.
/// </summary>
public enum UploadStatus
{
    /// <summary>Selected and validated, not handed to the transfer yet.</summary>
    Pending,

    /// <summary>Being transferred; <c>UploadFile.Percent</c> drives the bar.</summary>
    Uploading,

    /// <summary>Transfer completed.</summary>
    Success,

    /// <summary>Rejected by a component limit, or failed in the host's transfer.</summary>
    Error
}
