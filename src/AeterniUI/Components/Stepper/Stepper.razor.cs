using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Stepper;

/// <summary>A host-controlled step in a linear process.</summary>
/// <param name="Id">Stable identifier used by <see cref="Stepper.Value"/>.</param>
/// <param name="Label">Visible and accessible step name.</param>
/// <param name="Description">Optional supporting text shown below the label.</param>
/// <param name="Completed">Whether the host has completed this step.</param>
/// <param name="Disabled">Whether this step cannot be activated.</param>
/// <param name="Visible">Whether this step is rendered.</param>
public sealed record StepperItem(
    string Id,
    string Label,
    string? Description = null,
    bool Completed = false,
    bool Disabled = false,
    bool Visible = true);

/// <summary>
/// Displays the current position in a host-owned linear process. The component
/// does not advance business state or infer completion; bind <see cref="Value"/>
/// and provide each item's <see cref="StepperItem.Completed"/> explicitly.
/// </summary>
public partial class Stepper : AeterniComponent
{
    [Parameter, EditorRequired]
    public IReadOnlyList<StepperItem> Items { get; set; } = [];

    /// <summary>Controlled current step identifier.</summary>
    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Raised after an enabled step is activated.</summary>
    [Parameter]
    public EventCallback<StepperItem> OnStepClick { get; set; }

    private IReadOnlyList<StepperItem> VisibleItems => Items.Where(item => item.Visible).ToArray();

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel)
        ? UiText.StepperLabel
        : AriaLabel.Trim();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown stepper orientation.");
        }

        if (Items.Any(item => item is null))
        {
            throw new ArgumentException("Stepper items cannot be null.", nameof(Items));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("Stepper item ids cannot be empty.", nameof(Items));
            }

            if (string.IsNullOrWhiteSpace(item.Label))
            {
                throw new ArgumentException("Stepper item labels cannot be empty.", nameof(Items));
            }

            if (!ids.Add(item.Id))
            {
                throw new ArgumentException($"Stepper item id '{item.Id}' is duplicated.", nameof(Items));
            }
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-stepper")
        .Add("aeterni-stepper--vertical", Orientation == Orientation.Vertical)
        .Add("is-disabled", Disabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "group",
            ["aria-label"] = EffectiveAriaLabel
        };

        if (Disabled)
        {
            attributes["aria-disabled"] = "true";
            attributes["inert"] = true;
        }

        return attributes;
    }

    internal bool IsCurrent(StepperItem item) => string.Equals(Value, item.Id, StringComparison.Ordinal);

    internal bool IsItemDisabled(StepperItem item) => Disabled || item.Disabled;

    internal string ItemId(int index) => $"{ElementId}-step-{index}";

    internal string DescriptionId(int index) => $"{ItemId(index)}-description";

    private async Task SelectAsync(StepperItem item)
    {
        if (IsItemDisabled(item) || IsCurrent(item))
        {
            return;
        }

        await ValueChanged.InvokeAsync(item.Id);
        await OnStepClick.InvokeAsync(item);
    }
}
