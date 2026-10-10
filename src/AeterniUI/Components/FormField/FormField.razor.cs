using System.Linq.Expressions;
using AeterniUI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components.FormField;

public partial class FormField : AeterniComponent
{
    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [Parameter]
    public string? Label { get; set; }
    [Parameter]
    public string? Description { get; set; }
    [Parameter]
    public string? Error { get; set; }
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    public bool Required { get; set; }
    [Parameter]
    public bool Invalid { get; set; }
    [Parameter]
    public Expression<Func<object?>>? For { get; set; }

    private readonly EditContextSubscription _validation;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    public FormField() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    private string InputId => $"{ElementId}-input";

    /// <summary>
    /// Id of the rendered label, exposed through <see cref="FormFieldContext"/>
    /// so container-type controls (ComboBox, Rating, RadioGroup) can name
    /// themselves with <c>aria-labelledby</c>.
    /// </summary>
    private string? LabelId => string.IsNullOrWhiteSpace(Label) ? null : $"{ElementId}-label";

    private string DescriptionId => $"{ElementId}-description";
    private string ErrorId => $"{ElementId}-error";
    private string? ErrorMessage => !string.IsNullOrWhiteSpace(Error)
        ? Error.Trim()
        : _hasFieldIdentifier ? _validation.FirstValidationMessage(_fieldIdentifier) : null;
    private bool IsInvalid => Invalid || ErrorMessage is not null;
    private string? DescribedBy => string.Join(" ", new[] { string.IsNullOrWhiteSpace(Description) ? null : DescriptionId, ErrorMessage is null ? null : ErrorId }.Where(x => x is not null));
    private FormFieldContext Context => new(InputId, LabelId, DescribedBy, Disabled, IsInvalid, Required);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (For is not null && CascadedEditContext is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(For);
            _hasFieldIdentifier = true;
            _validation.Attach(CascadedEditContext);
        }
        else
        {
            _hasFieldIdentifier = false;
            Unsubscribe();
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-form-field")
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", Disabled);

    protected override ValueTask OnComponentDisposeAsync()
    {
        Unsubscribe();
        return ValueTask.CompletedTask;
    }

    private void Unsubscribe() => _validation.Detach();
}
