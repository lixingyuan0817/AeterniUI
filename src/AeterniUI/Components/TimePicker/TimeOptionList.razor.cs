using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.TimePicker;

/// <summary>Internal time option surface shared by TimePicker and DateTimePicker.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class TimeOptionList : AeterniComponent
{
    [Parameter, EditorRequired] public IReadOnlyList<TimeOnly> Options { get; set; } = [];
    [Parameter] public TimeOnly? Value { get; set; }
    [Parameter] public EventCallback<TimeOnly> ValueSelected { get; set; }
    [Parameter, EditorRequired] public string DisplayFormat { get; set; } = "HH:mm";
    [Parameter] public string AriaLabel { get; set; } = "Time options";
    [Parameter] public string EmptyText { get; set; } = "No available times";

    internal CultureInfo Culture => CultureInfo.CurrentCulture;

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-time-option-list");

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "listbox",
            ["aria-label"] = AriaLabel
        };
        return attributes;
    }

    private Task SelectAsync(TimeOnly value) => ValueSelected.InvokeAsync(value);
}
