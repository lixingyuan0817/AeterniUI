using System.Text.RegularExpressions;
using AeterniUI.Components.IconButtonMenu;
using AeterniUI.Components.Menu;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Contract checks for <c>IconButtonMenu</c> and <c>Menu.Flat</c> (10.42.0).
/// Pins the two halves of the delivery: the flattened list must lose the group
/// toggle / aria-expanded machinery without losing entry semantics, and the
/// composite must keep the base root contract while wiring its trigger to the
/// real menu root.
/// </summary>
internal static class IconButtonMenuChecks
{
    private static readonly IReadOnlyList<MenuGroup> Groups =
    [
        new("main", "Main", [
            new MenuItem("edit", "Edit", "Change the current record", Href: "/edit", Target: "_blank"),
            new MenuItem("hidden", "Hidden", Visible: false),
            new MenuItem("locked", "Locked", Disabled: true),
            new MenuItem("delete", "Delete", "Delete for good")
        ], InitiallyOpen: false),
        new("hidden-group", "Hidden group", [
            new MenuItem("gone", "Gone")
        ], Visible: false),
        new("danger", "Danger", [
            new MenuItem("remove", "Remove", "Delete the selection")
        ], Disabled: true)
    ];

    public static async Task RunAsync(HtmlRenderer renderer, List<string> failures)
    {
        void Require(bool condition, string message)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }

        async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters)
            where TComponent : IComponent
        {
            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
                return output.ToHtmlString();
            });
        }

        async Task RequireThrowAsync<TException>(IDictionary<string, object?> parameters, string expectedParameter, string what)
            where TException : ArgumentException
        {
            try
            {
                await RenderAsync<IconButtonMenu>(parameters);
                Require(false, $"IconButtonMenu must reject {what} instead of rendering.");
            }
            catch (Exception exception)
            {
                Require(exception.GetType() == typeof(TException) && ((ArgumentException)exception).ParamName == expectedParameter,
                    $"IconButtonMenu must reject {what} with {typeof(TException).Name} naming {expectedParameter}.");
            }
        }

        async Task RequireEnumRejectedAsync<TEnum>(Dictionary<string, object?> parameters, string expectedParameter, TEnum invalid)
            where TEnum : struct, Enum
        {
            try
            {
                await RenderAsync<IconButtonMenu>(parameters);
                Require(false, $"IconButtonMenu must reject the undefined {expectedParameter} {invalid}.");
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Require(exception.ParamName == expectedParameter && Equals(exception.ActualValue, invalid),
                    $"IconButtonMenu must reject an undefined {expectedParameter} carrying the actual value.");
            }
            catch (Exception exception)
            {
                Require(false, $"IconButtonMenu must reject an undefined {expectedParameter} with ArgumentOutOfRangeException, got {exception.GetType().Name}.");
            }
        }

        // --- Menu.Flat ------------------------------------------------------
        var flat = await RenderAsync<Menu>(new Dictionary<string, object?>
        {
            [nameof(Menu.Id)] = "contract-flat-menu",
            [nameof(Menu.Items)] = Groups,
            [nameof(Menu.Flat)] = true,
            [nameof(Menu.SelectedId)] = "edit"
        });

        Require(Regex.IsMatch(flat, "<nav[^>]*class=\"[^\"]*aeterni-menu[^\"]*is-flat[^\"]*\""),
            "Menu.Flat must mark the root with the is-flat state class.");
        Require(!flat.Contains("aeterni-menu__group", StringComparison.Ordinal),
            "Menu.Flat must not render group wrappers.");
        Require(!flat.Contains("group-toggle", StringComparison.Ordinal),
            "Menu.Flat must not render disclosure toggles.");
        Require(!flat.Contains("aeterni-menu__collapse", StringComparison.Ordinal),
            "Menu.Flat must not render collapse regions.");
        Require(!flat.Contains("aria-expanded", StringComparison.Ordinal) && !flat.Contains("aria-controls", StringComparison.Ordinal),
            "Menu.Flat must not emit disclosure ARIA state.");
        Require(!flat.Contains("<ul class=\"aeterni-menu__items\"", StringComparison.Ordinal),
            "Menu.Flat must render one flat list instead of per-group item lists.");

        // One row per visible entry across every group: hidden items, hidden
        // groups and their entries disappear, disabled entries stay rendered.
        // The isolated stylesheet appends a scope attribute after the class, so the
        // row pattern must not assume `class` is the last attribute.
        var flatRows = Regex.Matches(flat, "<li class=\"aeterni-menu__item-row\"[^>]*>", RegexOptions.None).Count;
        Require(flatRows == 4,
            $"Menu.Flat must render one row per visible entry (expected 4, got {flatRows}).");
        Require(flat.Contains(">Edit</span>", StringComparison.Ordinal)
            && flat.Contains(">Delete</span>", StringComparison.Ordinal)
            && flat.Contains(">Locked</span>", StringComparison.Ordinal)
            && flat.Contains(">Remove</span>", StringComparison.Ordinal),
            "Menu.Flat must keep every visible entry across the groups.");
        Require(!flat.Contains(">Hidden</span>", StringComparison.Ordinal)
            && !flat.Contains(">Gone</span>", StringComparison.Ordinal)
            && !flat.Contains(">Hidden group</span>", StringComparison.Ordinal),
            "Menu.Flat must skip entries that are not visible.");

        // Entry semantics survive flattening: selected marker, link variant and
        // link rel, icon/description markup, disabled buttons.
        Require(Regex.Matches(flat, "aria-current=\"page\"", RegexOptions.None).Count == 1,
            "Menu.Flat must keep exactly the selected entry marked with aria-current.");
        var selectedLink = Regex.Match(flat, "<a\\b[^>]*aria-current=\"page\"[^>]*>").Value;
        Require(selectedLink.Contains("href=\"/edit\"", StringComparison.Ordinal)
            && selectedLink.Contains("rel=\"noopener noreferrer\"", StringComparison.Ordinal),
            "Menu.Flat must keep the link variant and the new-tab rel of a MenuItem.Href entry.");
        Require(flat.Contains("aeterni-menu__item-copy", StringComparison.Ordinal)
            && Regex.IsMatch(flat, "<small[^>]*>Change the current record</small>"),
            "Menu.Flat must keep the description markup of an entry.");
        Require(Regex.IsMatch(flat, "<button[^>]*id=\"contract-flat-menu-group-0-item-2\"[^>]*\\sdisabled(?:=|\\s|>)"),
            "Menu.Flat must keep a disabled entry disabled.");
        Require(Regex.IsMatch(flat, "<button[^>]*id=\"contract-flat-menu-group-2-item-0\"[^>]*\\sdisabled(?:=|\\s|>)"),
            "Menu.Flat must inherit MenuGroup.Disabled onto its entries.");

        // Flat=false keeps the existing disclosure contract byte-for-byte.
        var grouped = await RenderAsync<Menu>(new Dictionary<string, object?>
        {
            [nameof(Menu.Id)] = "contract-grouped-menu",
            [nameof(Menu.Items)] = Groups
        });
        Require(grouped.Contains("aeterni-menu__group-toggle", StringComparison.Ordinal)
            && grouped.Contains("aria-expanded=\"false\"", StringComparison.Ordinal)
            && grouped.Contains("aria-expanded=\"true\"", StringComparison.Ordinal),
            "Menu without Flat must keep its group toggles and aria-expanded state.");
        Require(grouped.Contains("aeterni-menu__collapse", StringComparison.Ordinal)
            && grouped.Contains("aria-controls", StringComparison.Ordinal),
            "Menu without Flat must keep disclosure regions and aria-controls.");
        Require(!grouped.Contains("is-flat", StringComparison.Ordinal),
            "Menu without Flat must not emit the flat state class.");

        // --- IconButtonMenu -------------------------------------------------
        RenderFragment icon = builder =>
        {
            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "viewBox", "0 0 16 16");
            builder.CloseElement();
        };

        var html = await RenderAsync<IconButtonMenu>(new Dictionary<string, object?>
        {
            [nameof(IconButtonMenu.Id)] = "contract-icon-menu",
            [nameof(IconButtonMenu.Class)] = "consumer-icon-menu",
            [nameof(IconButtonMenu.Style)] = "margin: 0",
            [nameof(IconButtonMenu.AdditionalAttributes)] = new Dictionary<string, object>
            {
                ["data-contract"] = "icon-button-menu"
            },
            [nameof(IconButtonMenu.Icon)] = icon,
            [nameof(IconButtonMenu.Items)] = Groups,
            [nameof(IconButtonMenu.AriaLabel)] = "More actions",
            [nameof(IconButtonMenu.Flat)] = true
        });

        var root = Regex.Match(html, "<div\\b[^>]*>").Value;
        Require(root.Contains("id=\"contract-icon-menu\"", StringComparison.Ordinal)
            && root.Contains("aeterni-icon-button-menu", StringComparison.Ordinal)
            && root.Contains("consumer-icon-menu", StringComparison.Ordinal)
            && root.Contains("margin: 0", StringComparison.Ordinal)
            && root.Contains("data-contract=\"icon-button-menu\"", StringComparison.Ordinal),
            "IconButtonMenu root must preserve the base DOM contract (id, class, style, unmatched attributes).");

        var trigger = Regex.Match(html, "<button\\b[^>]*id=\"contract-icon-menu-trigger\"[^>]*>").Value;
        Require(trigger.Contains("aeterni-icon-button", StringComparison.Ordinal)
            && trigger.Contains("aria-label=\"More actions\"", StringComparison.Ordinal),
            "IconButtonMenu must delegate the trigger to IconButton with the required accessible name.");
        Require(trigger.Contains("aria-haspopup=\"menu\"", StringComparison.Ordinal)
            && trigger.Contains("aria-expanded=\"false\"", StringComparison.Ordinal),
            "IconButtonMenu trigger must announce the menu popup and its collapsed state.");

        var controlledId = Regex.Match(trigger, "aria-controls=\"([^\"]+)\"").Groups[1].Value;
        Require(!string.IsNullOrEmpty(controlledId) && Regex.IsMatch(html, $"<nav[^>]*id=\"{Regex.Escape(controlledId)}\""),
            "IconButtonMenu aria-controls must resolve to the real menu root.");
        Require(html.Contains("aeterni-menu is-flat", StringComparison.Ordinal)
            && !html.Contains("aeterni-menu__group", StringComparison.Ordinal)
            && !html.Contains("group-toggle", StringComparison.Ordinal),
            "IconButtonMenu must forward Flat to its inner Menu.");

        Require(html.Contains("--aeterni-popover-content-padding: var(--aeterni-spacing-1)", StringComparison.Ordinal)
            && html.Contains("--aeterni-menu-item-padding: var(--aeterni-control-padding-x-sm)", StringComparison.Ordinal),
            "IconButtonMenu must scope the compact MenuButton surface and row padding.");
        Require(!html.Contains("aeterni-list", StringComparison.Ordinal),
            "IconButtonMenu renders Menu, not List.");
        Require(Regex.IsMatch(html, "<div[^>]*aeterni-popover[^>]*\\shidden(?:=|\\s|>)"),
            "A closed IconButtonMenu must keep its popover hidden.");

        // Open is the controlled value, so it only drives the rendered state once
        // the change callback is bound — the same contract as MenuButton.
        var open = await RenderAsync<IconButtonMenu>(new Dictionary<string, object?>
        {
            [nameof(IconButtonMenu.Id)] = "contract-icon-menu-open",
            [nameof(IconButtonMenu.Icon)] = icon,
            [nameof(IconButtonMenu.Items)] = Groups,
            [nameof(IconButtonMenu.AriaLabel)] = "More actions",
            [nameof(IconButtonMenu.Open)] = true,
            [nameof(IconButtonMenu.OpenChanged)] = EventCallback.Factory.Create<bool>(new object(), _ => { })
        });
        var openTrigger = Regex.Match(open, "<button\\b[^>]*id=\"contract-icon-menu-open-trigger\"[^>]*>").Value;
        Require(openTrigger.Contains("aria-expanded=\"true\"", StringComparison.Ordinal),
            "IconButtonMenu must reflect a controlled open state on aria-expanded.");
        Require(!Regex.IsMatch(open, "<div[^>]*aeterni-popover[^>]*\\shidden(?:=|\\s|>)"),
            "An open IconButtonMenu must not keep its popover hidden.");

        var groupedComposite = await RenderAsync<IconButtonMenu>(new Dictionary<string, object?>
        {
            [nameof(IconButtonMenu.Icon)] = icon,
            [nameof(IconButtonMenu.Items)] = Groups,
            [nameof(IconButtonMenu.AriaLabel)] = "Grouped actions"
        });
        Require(groupedComposite.Contains("aeterni-menu__group-toggle", StringComparison.Ordinal)
            && !groupedComposite.Contains("is-flat", StringComparison.Ordinal),
            "IconButtonMenu without Flat must keep the grouped Menu contract.");

        var disabledComposite = await RenderAsync<IconButtonMenu>(new Dictionary<string, object?>
        {
            [nameof(IconButtonMenu.Icon)] = icon,
            [nameof(IconButtonMenu.Items)] = Groups,
            [nameof(IconButtonMenu.AriaLabel)] = "Disabled actions",
            [nameof(IconButtonMenu.Disabled)] = true
        });
        Require(Regex.IsMatch(disabledComposite, "<div\\b[^>]*aria-disabled=\"true\""),
            "IconButtonMenu must expose its disabled composite state on the root.");
        Require(Regex.IsMatch(disabledComposite, "<button\\b[^>]*\\sdisabled(?:=|\\s|>)"),
            "IconButtonMenu must disable the real trigger when Disabled is set.");

        // Defaults are the documented simplest usage.
        var defaults = new IconButtonMenu();
        Require(defaults.Variant == ButtonVariant.Ghost
            && defaults.Intent == ButtonIntent.Neutral
            && defaults.Size == Size.Default
            && defaults.Placement == PopupPlacement.BottomEnd
            && !defaults.Flat
            && !defaults.Loading
            && !defaults.Open,
            "IconButtonMenu defaults must be Ghost + Neutral + Default size + BottomEnd placement, flat off.");

        // Parameter validation: the accessible name is mandatory, and every
        // public enum rejects undefined values with the actual value attached.
        Dictionary<string, object?> Valid(Action<Dictionary<string, object?>>? configure = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["Icon"] = icon,
                ["Items"] = Groups,
                ["AriaLabel"] = "Actions"
            };
            configure?.Invoke(parameters);
            return parameters;
        }

        foreach (var blank in new[] { "", "   " })
        {
            await RequireThrowAsync<ArgumentException>(Valid(parameters => parameters[nameof(IconButtonMenu.AriaLabel)] = blank),
                nameof(IconButtonMenu.AriaLabel), $"the blank AriaLabel \"{blank}\"");
        }

        await RequireThrowAsync<ArgumentNullException>(Valid(parameters => parameters[nameof(IconButtonMenu.Items)] = null),
            nameof(IconButtonMenu.Items), "a null Items list");
        await RequireThrowAsync<ArgumentNullException>(Valid(parameters => parameters[nameof(IconButtonMenu.Icon)] = null),
            nameof(IconButtonMenu.Icon), "a null Icon");

        var invalidVariant = (ButtonVariant)99;
        var invalidIntent = (ButtonIntent)99;
        var invalidSize = (Size)99;
        var invalidPlacement = (PopupPlacement)99;

        await RequireEnumRejectedAsync(
            Valid(parameters => parameters[nameof(IconButtonMenu.Variant)] = invalidVariant),
            nameof(IconButtonMenu.Variant), invalidVariant);
        await RequireEnumRejectedAsync(
            Valid(parameters => parameters[nameof(IconButtonMenu.Intent)] = invalidIntent),
            nameof(IconButtonMenu.Intent), invalidIntent);
        await RequireEnumRejectedAsync(
            Valid(parameters => parameters[nameof(IconButtonMenu.Size)] = invalidSize),
            nameof(IconButtonMenu.Size), invalidSize);
        await RequireEnumRejectedAsync(
            Valid(parameters => parameters[nameof(IconButtonMenu.Placement)] = invalidPlacement),
            nameof(IconButtonMenu.Placement), invalidPlacement);
    }
}
