using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Popup;

public partial class PopupHost : AeterniComponent
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-popup-host");
}
