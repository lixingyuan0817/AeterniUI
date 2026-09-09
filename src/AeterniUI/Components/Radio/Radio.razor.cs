using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeterniUI.Components.Radio;

public partial class Radio<TValue> : AeterniComponent
{
    [CascadingParameter] private RadioGroupContext? Group { get; set; }
    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue?>>? ValueExpression { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Name { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Invalid { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string? AriaDescribedBy { get; set; }

    private string InputId => Id ?? InstanceId;
    private bool IsChecked => Group?.IsSelected(Value) ?? false;
    private bool IsDisabled => Disabled || (Group?.Disabled ?? false);
    private string? EffectiveName => Name ?? Group?.Name;
    private bool EffectiveRequired => Required || (Group?.Required ?? false);
    private bool EffectiveInvalid => Invalid || (Group?.Invalid ?? false);

    protected override bool SupportsDisabled => true;

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(EffectiveName)) attributes["name"] = EffectiveName!;
        if (EffectiveRequired) attributes["aria-required"] = "true";
        if (EffectiveInvalid) attributes["aria-invalid"] = "true";
        if (!string.IsNullOrWhiteSpace(AriaLabel)) attributes["aria-label"] = AriaLabel!;
        if (!string.IsNullOrWhiteSpace(AriaDescribedBy)) attributes["aria-describedby"] = AriaDescribedBy!;
        return attributes;
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-radio").Add("is-checked", IsChecked).Add("is-disabled", IsDisabled);

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
    public RadioGroupContext(Func<object?, bool> isSelected, Func<object?, Task> select, bool disabled, bool required, bool invalid, string? name)
    { _isSelected = isSelected; _select = select; Disabled = disabled; Required = required; Invalid = invalid; Name = name; }
    public bool IsSelected(object? value) => _isSelected(value);
    public Task SelectAsync(object? value) => _select(value);
}
