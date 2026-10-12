using System.Globalization;
using System.Linq.Expressions;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using AeterniUI.Icons;
using AeterniUI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components.Upload;

/// <summary>
/// Collects the files a user picks or drops. The component owns the selection,
/// the limits it can check without reading a byte, and the file list; the
/// transfer, its protocol and its storage stay with the host, which reports
/// progress back through <see cref="UploadFile.Status"/>.
/// </summary>
[JsModule("Components/Upload/Upload.razor.js", Name = "upload", Interactive = true)]
public partial class Upload : AeterniComponent
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public Upload() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public IReadOnlyList<UploadFile> SelectedFiles { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyList<UploadFile>> SelectedFilesChanged { get; set; }

    [Parameter]
    public Expression<Func<IReadOnlyList<UploadFile>>>? SelectedFilesExpression { get; set; }

    /// <summary>Allows more than one file per selection; required for a drop of several files.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// Filter handed to the native picker. It does not constrain a drop: the
    /// browser applies <c>accept</c> only to its own dialog, so the component
    /// reports what was dropped and leaves type checking to the host.
    /// </summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>Rejects files larger than this many bytes.</summary>
    [Parameter]
    public long? MaxFileSize { get; set; }

    /// <summary>Rejects files once this many accepted files exist.</summary>
    [Parameter]
    public int? MaxFileCount { get; set; }

    /// <summary>
    /// Renders the per-file remove action. On by default: without it a wrongly
    /// chosen file could never be taken back, and the list would only ever grow.
    /// </summary>
    [Parameter]
    public bool AllowRemove { get; set; } = true;

    /// <summary>
    /// Replaces the built-in row content (name, size, progress). The remove action
    /// stays with the component, so a template cannot lock the user out of
    /// revoking a file.
    /// </summary>
    [Parameter]
    public RenderFragment<UploadFile>? ItemTemplate { get; set; }

    /// <summary>Raised for each accepted file, after <see cref="SelectedFilesChanged"/>.</summary>
    [Parameter]
    public EventCallback<UploadFile> OnFileSelected { get; set; }

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>Drop-zone text; leave empty to use <c>AeterniUITextOptions.UploadDropText</c>.</summary>
    [Parameter]
    public string? DropText { get; set; }

    /// <summary>Prefix of the remove action; leave empty to use <c>AeterniUITextOptions.UploadRemoveLabel</c>.</summary>
    [Parameter]
    public string? RemoveLabel { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private InputFile? _inputFile;
    private ElementReference _dropZoneElement;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    // The picker is the element a FormField labels, so it has to carry the field's
    // input id; the root keeps the component id that ElementId reports. Pointing
    // the label at the root instead would leave the label's `for` aimed at a div.
    private string InputElementId => FormField?.InputId ?? $"{ElementId}-input";

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsRequired => Required || (FormField?.Required ?? false);

    private bool IsInvalid => Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private string? EffectiveDescribedBy => string.Join(" ", new[] { AriaDescribedBy, FormField?.DescribedBy }
        .Where(value => !string.IsNullOrWhiteSpace(value)));

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.UploadLabel
        : AriaLabel.Trim();

    private string DropTextValue => string.IsNullOrWhiteSpace(DropText)
        ? UiText.UploadDropText
        : DropText.Trim();

    private string EffectiveRemoveLabel => string.IsNullOrWhiteSpace(RemoveLabel)
        ? UiText.UploadRemoveLabel
        : RemoveLabel.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ValidateParameters();
        ArgumentNullException.ThrowIfNull(SelectedFiles);
        UpdateEditContextSubscription();
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }

    // The zone is passed explicitly rather than looked up from RootElement: a
    // consumer-supplied Element parameter replaces RootElement, and the drop
    // listeners would then be attached to the wrong node.
    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync(
            "upload",
            "sync",
            InstanceId,
            _dropZoneElement,
            _inputFile?.Element,
            !IsDisabled,
            Multiple);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-upload")
        .Add(SizeClass)
        .Add("is-full-width", FullWidth)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", IsDisabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        // The status attribute lives on the picker, which is the control the user
        // operates; the root only mirrors the disabled state for styling hooks.
        if (IsDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private async Task HandleFilesSelectedAsync(InputFileChangeEventArgs args)
    {
        // The cap is not passed to the framework call: exceeding it must mark the
        // surplus files as rejected (so the user sees why) instead of throwing out
        // of the change handler. Only metadata is transferred here.
        await ApplySelectionAsync(args.GetMultipleFiles(int.MaxValue));
    }

    /// <summary>
    /// The selection pipeline, driven by the picker, by a drop and by the
    /// contract checks. Existing rows keep their identity and their host-reported
    /// transfer state; rejected files are appended with their reason rather than
    /// dropped silently.
    /// </summary>
    internal async Task ApplySelectionAsync(IReadOnlyList<IBrowserFile> files)
    {
        if (IsDisabled || files.Count == 0)
        {
            return;
        }

        var next = SelectedFiles.ToList();
        var accepted = new List<UploadFile>();

        foreach (var content in Multiple ? files : files.Take(1))
        {
            var file = new UploadFile(content);

            if (MaxFileSize is { } maxSize && content.Size > maxSize)
            {
                file.Status = UploadStatus.Error;
                file.ErrorMessage = string.Format(CultureInfo.CurrentCulture, UiText.UploadFileTooLargeErrorFormat, FormatSize(maxSize));
            }
            else if (MaxFileCount is { } maxCount && next.Count + accepted.Count >= maxCount)
            {
                file.Status = UploadStatus.Error;
                file.ErrorMessage = string.Format(CultureInfo.CurrentCulture, UiText.UploadTooManyFilesErrorFormat, maxCount);
            }
            else
            {
                accepted.Add(file);
            }

            next.Add(file);
        }

        await SetSelectedFilesAsync(next);

        foreach (var file in accepted)
        {
            if (OnFileSelected.HasDelegate)
            {
                await OnFileSelected.InvokeAsync(file);
            }
        }
    }

    internal async Task RemoveAsync(UploadFile file)
    {
        if (IsDisabled || !AllowRemove)
        {
            return;
        }

        var next = SelectedFiles.Where(item => !string.Equals(item.Id, file.Id, StringComparison.Ordinal)).ToArray();
        if (next.Length == SelectedFiles.Count)
        {
            return;
        }

        await SetSelectedFilesAsync(next);
    }

    private async Task SetSelectedFilesAsync(IReadOnlyList<UploadFile> next)
    {
        SelectedFiles = next;

        if (SelectedFilesChanged.HasDelegate)
        {
            await SelectedFilesChanged.InvokeAsync(next);
        }

        if (_hasFieldIdentifier)
        {
            _validation.NotifyFieldChanged(_fieldIdentifier);
        }

        await InvokeAsync(StateHasChanged);
    }

    private string ItemClass(UploadFile file) => string.Join(" ", new[]
    {
        "aeterni-upload__item",
        ComponentClass.ForColor("aeterni-upload__item", StatusColor(file))
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private string StatusText(UploadFile file) => file.Status switch
    {
        UploadStatus.Uploading => UiText.UploadStatusUploadingLabel,
        UploadStatus.Success => UiText.UploadStatusSuccessLabel,
        UploadStatus.Error => string.IsNullOrWhiteSpace(file.ErrorMessage)
            ? UiText.UploadStatusErrorLabel
            : file.ErrorMessage,
        _ => UiText.UploadStatusPendingLabel
    };

    private static Color StatusColor(UploadFile file) => file.Status switch
    {
        UploadStatus.Uploading => Color.Primary,
        UploadStatus.Success => Color.Success,
        UploadStatus.Error => Color.Danger,
        _ => Color.Neutral
    };

    // Sizes are shown as the smallest unit that still reads as a whole number, so
    // a 900-byte file does not render as "0 MB".
    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => string.Format(CultureInfo.CurrentCulture, "{0} B", bytes),
        < 1024 * 1024 => string.Format(CultureInfo.CurrentCulture, "{0:0.#} KB", bytes / 1024d),
        < 1024L * 1024 * 1024 => string.Format(CultureInfo.CurrentCulture, "{0:0.#} MB", bytes / (1024d * 1024d)),
        _ => string.Format(CultureInfo.CurrentCulture, "{0:0.#} GB", bytes / (1024d * 1024d * 1024d))
    };

    private string? SizeClass => ComponentClass.ForSize("aeterni-upload", Size);

    private void UpdateEditContextSubscription()
    {
        _validation.Attach(SelectedFilesExpression is null ? null : CascadedEditContext);

        if (SelectedFilesExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(SelectedFilesExpression);
            _hasFieldIdentifier = true;
        }
        else
        {
            _hasFieldIdentifier = false;
        }
    }

    private void ValidateParameters()
    {
        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown upload size.");
        }

        if (MaxFileSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFileSize), MaxFileSize, "The maximum file size must be positive.");
        }

        if (MaxFileCount is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxFileCount), MaxFileCount, "The maximum file count must be at least one.");
        }

        if (!Multiple && MaxFileCount > 1)
        {
            throw new ArgumentException("A single-file upload cannot accept more than one file; enable Multiple first.", nameof(MaxFileCount));
        }
    }
}
