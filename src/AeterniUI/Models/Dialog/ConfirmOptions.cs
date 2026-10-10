namespace AeterniUI.Models.Dialog;

public sealed class ConfirmOptions : DialogOptions
{
    /// <summary>
    /// Label of the confirm action. An empty or null value falls back to
    /// <see cref="AeterniUITextOptions.ConfirmAcceptLabel" />, the same contract
    /// <see cref="AlertOptions.CloseText" /> uses (REV-156).
    /// </summary>
    public string? ConfirmText { get; set; }

    /// <summary>
    /// Label of the cancel action. An empty or null value falls back to
    /// <see cref="AeterniUITextOptions.ConfirmCancelLabel" /> (REV-156).
    /// </summary>
    public string? CancelText { get; set; }
}
