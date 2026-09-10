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
    private bool IsChecked => Group?.IsSelected(Value) ?? false;
    private bool IsDisabled => Disabled || (Group?.Disabled ?? false);
    private string? EffectiveName => Name ?? Group?.Name;
    private bool EffectiveRequired => Required || (Group?.Required ?? false);
    private bool EffectiveInvalid => Invalid || (Group?.Invalid ?? false);
    private Size EffectiveSize => Size ?? Group?.Size ?? SizeValue.Default;

    protected override bool SupportsDisabled => true;

    /// <summary>
    /// Attributes for the root label element: the component class plus the
    /// inherited DOM attributes. The input carries its own radio semantics.
    /// </summary>
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

        if (EffectiveInvalid)
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
        .Add("is-invalid", EffectiveInvalid);

    private string SizeClass => EffectiveSize switch
    {
        SizeValue.Small => "aeterni-radio--sm",
        SizeValue.Large => "aeterni-radio--lg",
        _ => string.Empty
    };

    private async Task SelectAsync()
    {
        if (IsDisabled) return;
        if (Group is not null) await Group.SelectAsync(Value);
        else await ValueChanged.InvokeAsync(Value);
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
