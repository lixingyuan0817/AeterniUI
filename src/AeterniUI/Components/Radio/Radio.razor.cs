using System.Linq.Expressions;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SizeValue = AeterniUI.Enums.Size;

namespace AeterniUI.Components.Radio;

public partial class Radio<TValue> : AeterniComponent
{
    [CascadingParameter]
    private RadioGroupContext? Group { get; set; }
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }
    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    // Mirrors Checkbox: the standalone shape joins the cascading EditContext so its
    // ValueExpression is a real field binding instead of a decorative parameter.
    private readonly EditContextSubscription _validation;

    public Radio() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public TValue? Value { get; set; }
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Control size. Defaults to <see cref="Size.Default"/> and falls back to
    /// the value cascaded by <see cref="RadioGroup{TValue}"/> (which itself
    /// defaults to <see cref="Size.Medium"/>).
    /// </summary>
    [Parameter]
    public Size? Size { get; set; }

    [Parameter]
    public bool Required { get; set; }
    [Parameter]
    public bool Invalid { get; set; }
    [Parameter]
    public string? AriaLabel { get; set; }
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    // The root label owns the component id, so the input gets its own
    // "{id}-input" id (matching Checkbox and Switch). A standalone radio inside a
    // FormField adopts the field input id instead, so the field label's `for`
    // resolves to the input.
    private string InputId => (Group is null ? FormField?.InputId : null) ?? $"{ElementId}-input";

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;
    private Expression<Func<TValue?>>? _expressionSource;
    private Func<TValue?>? _expressionAccessor;

    // REV-133: grouped radios compare against the group value. A standalone radio has
    // no peer to compare with, so the display depends on the shape the host used:
    //  - with a ValueExpression (the EditForm shape) the option is checked when the
    //    bound value equals this option's Value, exactly like a grouped radio compares
    //    against the group value;
    //  - without one (@bind-Value / Value + ValueChanged) the legacy truthiness rule
    //    still drives the display — `true` for bool, non-empty for strings, anything
    //    but `default` otherwise — so an initial value, a programmatic assignment and
    //    a form reset all stayed visible (REV-133).
    private bool IsChecked => Group?.IsSelected(Value) ?? IsStandaloneChecked();
    private bool IsStandaloneChecked()
    {
        if (ValueExpression is not null)
        {
            return EqualityComparer<TValue?>.Default.Equals(Value, BoundValue());
        }

        return (object?)Value switch
        {
            bool flag => flag,
            string text => text.Length > 0,
            null => false,
            var other => !Equals(other, default(TValue))
        };
    }

    private TValue? BoundValue()
    {
        // Compiling on every render would build the same expression tree repeatedly;
        // the latest instance is cached, and a changed binding rebuilds it once.
        if (!ReferenceEquals(_expressionSource, ValueExpression))
        {
            _expressionSource = ValueExpression;
            _expressionAccessor = ValueExpression?.Compile();
        }

        return _expressionAccessor is null ? default : _expressionAccessor();
    }
    private bool IsDisabled => Disabled || (Group?.Disabled ?? false) || (FormField?.Disabled ?? false);
    private string? EffectiveName => Name ?? Group?.Name;
    private bool EffectiveRequired => Required || (Group?.Required ?? false) || (FormField?.Required ?? false);
    private bool EffectiveInvalid => Invalid || (Group?.Invalid ?? false) || (FormField?.Invalid ?? false);
    private Size EffectiveSize => Size ?? Group?.Size ?? SizeValue.Default;

    // A standalone radio's own ValueExpression contributes validation messages to the
    // invalid state, matching Checkbox. Grouped radios keep deferring to the group /
    // FormField state: their child expression is not the field owner.
    private bool IsInvalid =>
        EffectiveInvalid ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    // The root is a <label>, which cannot carry the native disabled attribute,
    // so SupportsDisabled stays false (matching Card/FlashCard/Card-based roots).
    // The child <input> owns the native disabled state; the label only reports
    // it through aria-disabled so assistive technology can still announce it.

    /// <summary>
    /// Attributes for the root label element: the component class plus the
    /// inherited DOM attributes. The input carries its own radio semantics.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();

        if (Size is { } sizeValue && !Enum.IsDefined(sizeValue))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown radio size.");
        }
    }

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (IsDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    /// <summary>
    /// Attributes for the native radio input. The root label owns the component
    /// class (and therefore the size modifiers), so this method only emits the
    /// input-specific semantics.
    /// </summary>
    private IReadOnlyDictionary<string, object> BuildInputAttributes()
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(EffectiveName))
        {
            attributes["name"] = EffectiveName!;
        }

        if (EffectiveRequired)
        {
            attributes["aria-required"] = "true";
        }

        if (IsInvalid)
        {
            attributes["aria-invalid"] = "true";
        }

        if (IsDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel!;
        }

        var describedBy = string.Join(" ", new[] { AriaDescribedBy, FormField?.DescribedBy }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(describedBy))
        {
            attributes["aria-describedby"] = describedBy;
        }

        return attributes;
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-radio")
        .Add(SizeClass)
        .Add("is-checked", IsChecked)
        .Add("is-disabled", IsDisabled)
        .Add("is-invalid", IsInvalid);

    private string SizeClass => EffectiveSize switch
    {
        SizeValue.Small => "aeterni-radio--sm",
        SizeValue.Large => "aeterni-radio--lg",
        _ => string.Empty
    };

    private async Task SelectAsync()
    {
        if (IsDisabled) return;

        if (Group is not null)
        {
            await Group.SelectAsync(Value);
            return;
        }

        await ValueChanged.InvokeAsync(Value);
        if (_hasFieldIdentifier) _validation.NotifyFieldChanged(_fieldIdentifier);
    }

    // A grouped radio lets RadioGroup own the bound value, so only the standalone
    // shape subscribes to a cascading EditContext. This is the same three-step
    // contract Checkbox uses: attach the context, keep the FieldIdentifier built from
    // ValueExpression, and detach when the component goes away.
    private void UpdateEditContextSubscription()
    {
        _validation.Attach(Group is null && ValueExpression is not null ? CascadedEditContext : null);
        _hasFieldIdentifier = Group is null && ValueExpression is not null;
        if (_hasFieldIdentifier)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression!);
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        return ValueTask.CompletedTask;
    }
}

public sealed class RadioGroupContext
{
    private readonly Func<object?, bool> _isSelected;
    private readonly Func<object?, Task> _select;
    public bool Disabled { get; }
    public bool Required { get; }
    public bool Invalid { get; }
    public string? Name { get; }
    public Size? Size { get; }
    public RadioGroupContext(Func<object?, bool> isSelected, Func<object?, Task> select, bool disabled, bool required, bool invalid, string? name, Size? size = null)
    { _isSelected = isSelected; _select = select; Disabled = disabled; Required = required; Invalid = invalid; Name = name; Size = size; }
    public bool IsSelected(object? value) => _isSelected(value);
    public Task SelectAsync(object? value) => _select(value);
}
