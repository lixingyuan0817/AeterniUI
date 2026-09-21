using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AeterniUI.Components.ComboBox;
using AeterniUI.Components.FormField;
using AeterniUI.Components.Radio;
using AeterniUI.Components.Tabs;

public sealed class QualityContractHost : ComponentBase
{
    [Parameter] public Action<QualityContractHost>? Ready { get; set; }
    public bool Disabled, Invalid, Required, HideFirst;
    public bool ShowFirst = true;
    public string? Value = "a";
    public ComboBox<string> Combo = null!;
    public RadioGroup<string> Radios = null!;
    public Radio<string> Standalone = null!;
    public Tabs Tabs = null!;
    public void Update() => StateHasChanged();
    protected override void OnInitialized() => Ready?.Invoke(this);
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        b.OpenComponent<FormField>(0);
        b.AddAttribute(1, "Disabled", Disabled);
        b.AddAttribute(2, "Invalid", Invalid);
        b.AddAttribute(3, "Required", Required);
        b.AddAttribute(4, "Label", "Field state");
        b.AddAttribute(5, "ChildContent", (RenderFragment)(c => {
            c.OpenComponent<ComboBox<string>>(0);
            c.AddAttribute(1, "Items", Array.Empty<string>());
            c.AddAttribute(2, "EmptyContent", (RenderFragment)(t => t.AddContent(0, "Empty choices")));
            c.AddComponentReferenceCapture(3, x => Combo = (ComboBox<string>)x);
            c.CloseComponent();
            c.OpenComponent<RadioGroup<string>>(4);
            c.AddAttribute(5, "ChildContent", (RenderFragment)(r => {
                r.OpenComponent<Radio<string>>(0); r.AddAttribute(1, "Value", "a"); r.CloseComponent();
            }));
            c.AddComponentReferenceCapture(6, x => Radios = (RadioGroup<string>)x);
            c.CloseComponent();
            c.OpenComponent<Radio<string>>(7);
            c.AddAttribute(8, "Value", "standalone");
            c.AddComponentReferenceCapture(9, x => Standalone = (Radio<string>)x);
            c.CloseComponent();
        }));
        b.CloseComponent();
        b.OpenComponent<Tabs>(10);
        b.AddAttribute(11, "Value", Value);
        b.AddAttribute(12, "ValueChanged", EventCallback.Factory.Create<string?>(this, value => Value = value));
        b.AddAttribute(13, "ChildContent", (RenderFragment)(t => {
            if (ShowFirst) {
                t.OpenComponent<Tab>(0); t.SetKey("a");
                t.AddAttribute(1, "Value", "a"); t.AddAttribute(2, "Visible", !HideFirst);
                t.AddAttribute(3, "Header", (RenderFragment)(h => h.AddContent(0, "Header A")));
                t.AddAttribute(4, "ChildContent", (RenderFragment)(h => h.AddContent(0, "Panel A")));
                t.CloseComponent();
            }
            t.OpenComponent<Tab>(5); t.SetKey("b");
            t.AddAttribute(6, "Value", "b");
            t.AddAttribute(7, "Header", (RenderFragment)(h => h.AddContent(0, "Header B")));
            t.AddAttribute(8, "ChildContent", (RenderFragment)(h => h.AddContent(0, "Panel B")));
            t.CloseComponent();
        }));
        b.AddComponentReferenceCapture(14, x => Tabs = (Tabs)x);
        b.CloseComponent();
    }
}

public sealed class ParameterContractHost<TComponent> : ComponentBase where TComponent : IComponent
{
    [Parameter] public Action<ParameterContractHost<TComponent>>? Ready { get; set; }
    public Dictionary<string, object?> Values = [];
    public TComponent Inner = default!;
    public void Update() => StateHasChanged();
    protected override void OnInitialized() => Ready?.Invoke(this);
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        b.OpenComponent<TComponent>(0);
        b.AddMultipleAttributes(1, Values!);
        b.AddComponentReferenceCapture(2, c => Inner = (TComponent)c);
        b.CloseComponent();
    }
}
