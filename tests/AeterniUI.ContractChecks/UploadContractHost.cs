using AeterniUI.Components.FormField;
using AeterniUI.Components.Upload;
using AeterniUI.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Hosts an <see cref="Upload" /> inside a <see cref="FormField" /> so the
/// contract checks can assert that the field label targets the picker: the input
/// is the control a label must point at, and the component's root is a div.
/// </summary>
internal sealed class UploadContractHost : ComponentBase
{
    [Parameter]
    public Action<UploadContractHost>? Ready { get; set; }

    public IReadOnlyList<UploadFile> Files = [];

    /// <summary>Host-supplied description id, merged with the field's own.</summary>
    public string AriaDescribedBy = "host-hint";

    public Upload Field { get; private set; } = null!;

    public bool AllowRemove = true;

    protected override void OnInitialized() => Ready?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FormField>(0);
        builder.AddAttribute(1, "Label", "Attachments");
        builder.AddAttribute(2, "Description", "PDF or images up to 2 MB");
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<Upload>(0);
            content.AddAttribute(1, "SelectedFiles", Files);
            content.AddAttribute(2, "AriaDescribedBy", AriaDescribedBy);
            content.AddAttribute(3, "AllowRemove", AllowRemove);
            content.AddComponentReferenceCapture(4, component => Field = (Upload)component);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
