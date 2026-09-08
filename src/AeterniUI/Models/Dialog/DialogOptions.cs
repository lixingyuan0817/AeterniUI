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

    public RenderFragment? Footer { get; set; }
}
