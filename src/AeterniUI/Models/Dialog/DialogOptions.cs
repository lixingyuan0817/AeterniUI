using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Models.Dialog;

public class DialogOptions
{
    public string? Title { get; set; }

    public Severity Severity { get; set; } = Severity.Default;

    public RenderFragment? Icon { get; set; }

    public bool ShowCloseButton { get; set; } = true;

    public bool CloseOnEscape { get; set; } = true;

    public bool CloseOnOverlayClick { get; set; }

    /// <summary>
    /// Whether the provider focuses the first form field when the dialog opens. Any
    /// custom dialog can opt in: the first <c>input</c> / <c>select</c> / <c>textarea</c> /
    /// <c>contenteditable</c> field in the dialog body receives focus, and a dialog
    /// without such a field falls back to the normal first-focusable target. Defaults
    /// to <c>false</c> so ordinary content dialogs keep focusing their chrome.
    /// </summary>
    public bool AutofocusFirstField { get; set; }

    public RenderFragment? Footer { get; set; }
}
