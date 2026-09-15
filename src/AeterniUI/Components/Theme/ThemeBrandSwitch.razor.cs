using AeterniUI.Enums;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;

namespace AeterniUI.Components.Theme;

/// <summary>
/// Brand hue picker (Purple / Green). It is a thin adapter over
/// <see cref="Segmented{TValue}"/>, exactly like <see cref="ThemeSwitch"/>: the control
/// owns the radiogroup semantics and the sliding indicator, this component owns the
/// brand list, the localised labels and the ThemeService calls.
/// </summary>
public partial class ThemeBrandSwitch : AeterniComponent
{
    [Inject]
    protected ThemeService ThemeService { get; set; } = default!;

    /// <summary>
    /// Accessible name of the brand group. Defaults to
    /// <see cref="AeterniUITextOptions.ThemeBrandSwitchLabel"/>.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public EventCallback<ThemeBrand> BrandChanged { get; set; }

    protected override void OnComponentInitialized()
    {
        ThemeService.BrandChanged += HandleBrandChanged;
    }

    private string GroupLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.ThemeBrandSwitchLabel : AriaLabel;

    /// <summary>
    /// Option order of the switch. The order is part of the component contract: the brand
    /// labels follow it and so does the indicator's travel.
    /// </summary>
    private static readonly ThemeBrand[] Brands = [ThemeBrand.Purple, ThemeBrand.Green];

    private string BrandLabel(ThemeBrand brand) => brand switch
    {
        ThemeBrand.Green => UiText.ThemeBrandGreenLabel,
        _ => UiText.ThemeBrandPurpleLabel
    };

    /// <summary>
    /// Reports the control's root element as this component's own, so the base class
    /// parameters (<c>Element</c> / <c>ElementChanged</c>) describe the same element the
    /// consumer sees.
    /// </summary>
    private async Task ForwardElementChangedAsync(ElementReference element)
    {
        SetRootElement(element);

        if (ElementChanged.HasDelegate)
        {
            await ElementChanged.InvokeAsync(element);
        }
    }

    protected async Task SelectBrandAsync(ThemeBrand brand)
    {
        if (Disabled || ThemeService.Brand == brand)
        {
            return;
        }

        ThemeService.SetBrand(brand);

        if (BrandChanged.HasDelegate)
        {
            await BrandChanged.InvokeAsync(brand);
        }
    }

    protected override ValueTask OnComponentDisposeAsync()
    {
        ThemeService.BrandChanged -= HandleBrandChanged;
        return ValueTask.CompletedTask;
    }

    private void HandleBrandChanged(object? sender, EventArgs args)
    {
        if (!IsDisposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
