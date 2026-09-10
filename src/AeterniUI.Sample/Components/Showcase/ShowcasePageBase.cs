using AeterniUI.Enums;
using AeterniUI.Models.Dialog;
using AeterniUI.Services;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// State and helpers shared by more than one showcase page.
/// The demo pages are separate routes, so each page owns its own state in its
/// <c>@code</c> block; only members that several pages need live here (the two
/// injected services, the interaction note used by the Button demos and the
/// theme subscription).
/// </summary>
public abstract class ShowcasePageBase : ComponentBase, IDisposable
{
    [Inject]
    protected ThemeService ThemeService { get; set; } = default!;

    [Inject]
    protected IDialogService DialogService { get; set; } = default!;

    protected string _interactionNote = "点击预览组件即可查看事件反馈。";

    protected static readonly (string Label, Color Color)[] ColorFamilies =
    [
        ("Brand / Primary", Color.Primary),
        ("Success", Color.Success),
        ("Warning", Color.Warning),
        ("Danger", Color.Danger),
        ("Info", Color.Info),
        ("Neutral", Color.Neutral)
    ];

    /// <summary>Alert and Toast only expose the four semantic severities.</summary>
    protected static readonly (string Label, Color Color, Severity Severity)[] SeverityFamilies =
    [
        ("Success", Color.Success, Severity.Success),
        ("Warning", Color.Warning, Severity.Warning),
        ("Danger", Color.Danger, Severity.Danger),
        ("Info", Color.Info, Severity.Info)
    ];

    protected string _feedbackNote = "选择一种反馈类型查看 DialogService。";

    protected static readonly (Severity Severity, string Label, Color Color)[] FeedbackSeverities =
    [
        (Severity.Info, "Info", Color.Info),
        (Severity.Success, "Success", Color.Success),
        (Severity.Warning, "Warning", Color.Warning),
        (Severity.Danger, "Danger", Color.Danger)
    ];

    protected override void OnInitialized()
    {
        ThemeService.ThemeChanged += HandleThemeChanged;
    }

    protected void HandleThemeChanged(object? sender, EventArgs args) => _ = InvokeAsync(StateHasChanged);

    protected void MarkInteraction(MouseEventArgs _)
    {
        _interactionNote = $"已触发 OnClick：{DateTime.Now:HH:mm:ss}。";
    }

    public void Dispose()
    {
        ThemeService.ThemeChanged -= HandleThemeChanged;
    }
}
