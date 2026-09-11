using System.Globalization;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Segmented;

/// <summary>
/// Single-select segmented control. One option is active and the pill slides to it.
/// The single-select semantics come from native radio inputs, so arrow keys, the
/// one-tab-stop roving focus and the RTL arrow direction stay the browser's job;
/// the component adds the track, the sliding indicator and the FormField wiring.
/// </summary>
public partial class Segmented<TValue> : AeterniComponent
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    /// <summary>Options in display order. Every option gets the same width.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TValue> Items { get; set; } = [];

    /// <summary>
    /// Selected option. When it matches no item the control renders without a
    /// selection (and without the pill) instead of failing.
    /// </summary>
    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Control size tier.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    /// <summary>Fill the parent width instead of hugging the option content.</summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>Label text of an option. Falls back to the value's <c>ToString</c>.</summary>
    [Parameter]
    public Func<TValue, string>? TextSelector { get; set; }

    /// <summary>Custom option content (an icon plus a label, for example).</summary>
    [Parameter]
    public RenderFragment<TValue>? ItemTemplate { get; set; }

    /// <summary>Marks individual options as disabled. The group stays reachable.</summary>
    [Parameter]
    public Func<TValue, bool>? DisabledSelector { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Accessible name of the option group.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    // The radios need one shared name per instance to form a single native group.
    private string GroupName => $"{ElementId}-segmented";

    // Inside a FormField the group adopts the field's input id, so the field label's
    // `for` resolves to an element that exists.
    private string GroupId => Id ?? FormField?.InputId ?? InstanceId;

    private bool IsEffectivelyDisabled => Disabled || FormField?.Disabled == true;

    private bool IsEffectivelyRequired => Required || FormField?.Required == true;

    private bool IsEffectivelyInvalid => Invalid || FormField?.Invalid == true;

    private int SelectedIndex => Value is null ? -1 : IndexOf(Value);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown segmented size.");
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-segmented")
        .Add(ComponentClass.ForSize("aeterni-segmented", Size))
        .Add("is-full-width", FullWidth)
        .Add("is-disabled", IsEffectivelyDisabled)
        .Add("is-invalid", IsEffectivelyInvalid)
        .Add("is-empty", SelectedIndex < 0);

    // The pill is positioned from these two numbers, never from a hard-coded column
    // count: width is one column and the offset is `index` columns.
    protected override StyleBuilder BuildStyle() => base.BuildStyle()
        .Add("--aeterni-segmented-index", SelectedIndex.ToString(CultureInfo.InvariantCulture))
        .Add("--aeterni-segmented-count", Math.Max(Items.Count, 1).ToString(CultureInfo.InvariantCulture));

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = GroupId,
            ["role"] = "radiogroup",
            ["aria-orientation"] = "horizontal"
        };

        // Value-carrying ARIA states are emitted as strings, and only when they are
        // actually true (a false string adds noise, a bool renders as no value).
        if (IsEffectivelyDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        if (IsEffectivelyRequired)
        {
            attributes["aria-required"] = "true";
        }

        if (IsEffectivelyInvalid)
        {
            attributes["aria-invalid"] = "true";
        }

        if (!string.IsNullOrWhiteSpace(AriaLabel))
        {
            attributes["aria-label"] = AriaLabel.Trim();
        }

        if (FormField?.LabelId is { } labelId)
        {
            attributes["aria-labelledby"] = labelId;
        }

        if (FormField?.DescribedBy is { } describedBy)
        {
            attributes["aria-describedby"] = describedBy;
        }

        return attributes;
    }

    private string OptionClass(bool isSelected, bool isDisabled) => ClassBuilder("aeterni-segmented__option")
        .Add("is-selected", isSelected)
        .Add("is-disabled", isDisabled)
        .Build();

    private bool IsSelected(TValue item) => EqualityComparer<TValue>.Default.Equals(Value, item);

    private bool IsItemDisabled(TValue item) => IsEffectivelyDisabled || DisabledSelector?.Invoke(item) == true;

    private string ItemText(TValue item) => TextSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;

    private int IndexOf(TValue item)
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (EqualityComparer<TValue>.Default.Equals(Items[index], item))
            {
                return index;
            }
        }

        return -1;
    }

    private async Task SelectAsync(TValue item)
    {
        if (IsItemDisabled(item) || IsSelected(item))
        {
            return;
        }

        Value = item;

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(item);
        }
    }
}
