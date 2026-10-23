using System.Text.RegularExpressions;
using AeterniUI.Components.NavMenu;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Contract checks for the NavMenu enhancements (density tiers, row-level state
/// paint, child indent). Icon and description sizes are content concerns and must
/// not come back as NavMenu parameters.
/// </summary>
internal static class NavMenuEnhancementChecks
{
    public static async Task RunAsync(HtmlRenderer renderer, List<string> failures)
    {
        void Require(bool condition, string message)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }

        async Task<string> RenderAsync(IDictionary<string, object?> parameters)
        {
            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync<NavMenu>(ParameterView.FromDictionary(parameters));
                return output.ToHtmlString();
            });
        }

        static NavMenuNode[] Items() =>
        [
            new(
                "project",
                "Project",
                [
                    new("child-a", "Child A", Description: "Child A description"),
                    new("child-b", "Child B", Href: "/child-b")
                ],
                Trailing: builder => builder.AddContent(0, "trailing-actions")),
            new("leaf", "Leaf", Href: "/leaf"),
            new("disabled-leaf", "Disabled leaf", Disabled: true)
        ];

        static string RootOpeningTag(string html) => Regex.Match(html, "<nav\\b[^>]*>").Value;

        // Reads one rule body by selector prefix. The stylesheet is the only place
        // that can prove the state paint really moved from the button to the row.
        static string Rule(string css, string selector)
        {
            var index = css.IndexOf(selector, StringComparison.Ordinal);
            if (index < 0)
            {
                return string.Empty;
            }

            var open = css.IndexOf('{', index);
            var close = open < 0 ? -1 : css.IndexOf('}', open);
            return open < 0 || close < 0 ? string.Empty : css[(open + 1)..close];
        }

        // Defaults are the compatibility promise: an existing host that does not set
        // the options keeps the pre-tier markup and look unchanged.
        var defaults = new NavMenu();
        Require(defaults.Density == NavMenuDensity.Default
            && defaults.ChildIndent == NavMenuChildIndent.Default,
            "NavMenu Density/ChildIndent must default to their Default tiers.");

        // Icon and description sizes belong to the node content / RowTemplate. Keeping
        // them as NavMenu parameters would give the component a second size system
        // that the caller's own icon and description markup cannot see.
        Require(typeof(NavMenu).GetProperty("IconSize") is null
            && typeof(NavMenu).GetProperty("DescriptionSize") is null,
            "NavMenu must leave icon and description sizes to the content; "
            + "IconSize and DescriptionSize are not component parameters.");

        var html = await RenderAsync(new Dictionary<string, object?>
        {
            ["Id"] = "contract-nav-menu",
            ["Class"] = "consumer-nav-menu",
            ["Style"] = "max-width: 240px",
            ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "nav-menu" },
            ["Items"] = Items(),
            ["SelectedId"] = "child-a",
            ["OpenKeys"] = new[] { "project" },
            ["AriaLabel"] = "Contract navigation"
        });

        var root = RootOpeningTag(html);
        Require(root.Contains("id=\"contract-nav-menu\"", StringComparison.Ordinal)
            && root.Contains("consumer-nav-menu", StringComparison.Ordinal)
            && root.Contains("max-width: 240px", StringComparison.Ordinal)
            && root.Contains("data-contract=\"nav-menu\"", StringComparison.Ordinal)
            && root.Contains("aeterni-nav-menu", StringComparison.Ordinal)
            && root.Contains("aria-label=\"Contract navigation\"", StringComparison.Ordinal),
            "NavMenu must keep forwarding Id/Class/Style/unmatched attributes and AriaLabel to the nav root.");

        // Default tiers emit no modifier class at all (AGENTS §4.2).
        Require(!root.Contains("aeterni-nav-menu--", StringComparison.Ordinal),
            "NavMenu default tiers must not emit density/indent modifier classes.");

        // Row-level state: is-selected / is-disabled must land on the row container so
        // the Trailing region shares the paint with the label.
        Require(Regex.IsMatch(html, "<div class=\"aeterni-nav-menu__row[^\"]*is-selected", RegexOptions.None),
            "NavMenu must put is-selected on the row container, not only on the inner button.");
        Require(Regex.IsMatch(html, "<div class=\"aeterni-nav-menu__row[^\"]*is-disabled", RegexOptions.None),
            "NavMenu must put is-disabled on the row container of a disabled node.");

        var allDisabled = await RenderAsync(new Dictionary<string, object?>
        {
            ["Items"] = Items(),
            ["Disabled"] = true
        });
        var rowCount = Regex.Matches(allDisabled, "aeterni-nav-menu__row", RegexOptions.None).Count;
        var disabledRowCount = Regex.Matches(
            allDisabled,
            "<div class=\"aeterni-nav-menu__row[^\"]*is-disabled",
            RegexOptions.None).Count;
        Require(rowCount > 0 && rowCount == disabledRowCount,
            "NavMenu Disabled must mark every rendered row as is-disabled.");

        // Density tiers carry the root modifier class.
        foreach (var (density, expectedClass) in new (NavMenuDensity Density, string Class)[]
        {
            (NavMenuDensity.Compact, "aeterni-nav-menu--density-compact"),
            (NavMenuDensity.Comfortable, "aeterni-nav-menu--density-comfortable")
        })
        {
            var variant = await RenderAsync(new Dictionary<string, object?>
            {
                ["Items"] = Items(),
                ["Density"] = density
            });
            Require(RootOpeningTag(variant).Contains(expectedClass, StringComparison.Ordinal),
                $"NavMenu Density={density} must render the {expectedClass} root class.");
        }

        // Child indent tiers carry their own root modifier class.
        foreach (var (parameter, value, expectedClass) in new (string Parameter, object Value, string Class)[]
        {
            (nameof(NavMenu.ChildIndent), NavMenuChildIndent.Compact, "aeterni-nav-menu--indent-compact"),
            (nameof(NavMenu.ChildIndent), NavMenuChildIndent.Spacious, "aeterni-nav-menu--indent-spacious")
        })
        {
            var variant = await RenderAsync(new Dictionary<string, object?>
            {
                ["Items"] = Items(),
                [parameter] = value
            });
            Require(RootOpeningTag(variant).Contains(expectedClass, StringComparison.Ordinal),
                $"NavMenu {parameter}={value} must render the {expectedClass} root class.");
        }

        // Every tier is declared as a component-level variable and consumed exactly
        // once, so selectors never repeat the pixel values.
        var css = await File.ReadAllTextAsync(Path.Combine(
            FindRepositoryRoot(),
            "src/AeterniUI/Components/NavMenu/NavMenu.razor.css"));

        var baseRule = Rule(css, ".aeterni-nav-menu");
        Require(baseRule.Contains("--aeterni-nav-menu-row-height: var(--aeterni-row-height-compact)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-row-padding-block: 0", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-row-padding-inline: var(--aeterni-spacing-2)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-row-radius: var(--aeterni-radius-md)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-gap: var(--aeterni-spacing-1)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-font-size: var(--aeterni-font-size-sm)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-icon-size: var(--aeterni-icon-size-md)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-description-size: calc(var(--aeterni-font-size-xs) - 1px)", StringComparison.Ordinal)
            && baseRule.Contains("--aeterni-nav-menu-row-color: var(--aeterni-text-secondary)", StringComparison.Ordinal),
            "NavMenu default density must keep the 32px / 0 8 / 6px radius / 4px gap / 14px / 16px icon / secondary ink look.");

        Require(css.Contains("min-height: var(--aeterni-nav-menu-row-height)", StringComparison.Ordinal)
            && css.Contains("padding: var(--aeterni-nav-menu-row-padding-block) var(--aeterni-nav-menu-row-padding-inline)", StringComparison.Ordinal)
            && css.Contains("border-radius: var(--aeterni-nav-menu-row-radius)", StringComparison.Ordinal)
            && css.Contains("gap: var(--aeterni-nav-menu-gap)", StringComparison.Ordinal)
            && css.Contains("font-size: var(--aeterni-nav-menu-font-size)", StringComparison.Ordinal)
            && css.Contains("width: var(--aeterni-nav-menu-icon-size)", StringComparison.Ordinal)
            && css.Contains("font-size: var(--aeterni-nav-menu-description-size)", StringComparison.Ordinal)
            && css.Contains("margin-inline-start: var(--aeterni-nav-menu-child-margin)", StringComparison.Ordinal)
            && css.Contains("padding-inline-start: var(--aeterni-nav-menu-child-padding)", StringComparison.Ordinal)
            && css.Contains("color: var(--aeterni-nav-menu-row-color)", StringComparison.Ordinal),
            "Every NavMenu tier variable must have a consumer in the component stylesheet.");

        var nodeRule = Rule(css, ".aeterni-nav-menu__node");
        var openNodeRule = Rule(css, ".aeterni-nav-menu__node.is-open");
        Require(nodeRule.Contains("display: grid", StringComparison.Ordinal)
            && openNodeRule.Contains("gap: var(--aeterni-nav-menu-gap)", StringComparison.Ordinal),
            "An open NavMenu group must keep the list row gap before its first child "
            + "(the same group-to-items recipe Menu uses); otherwise a selected child and the "
            + "hovered group row above it paint touching backgrounds.");

        var comfortableRule = Rule(css, ".aeterni-nav-menu--density-comfortable");
        Require(comfortableRule.Contains("--aeterni-control-height-md", StringComparison.Ordinal)
            && comfortableRule.Contains("--aeterni-border-width) * 2", StringComparison.Ordinal)
            && comfortableRule.Contains("--aeterni-spacing-1) - 1px", StringComparison.Ordinal)
            && comfortableRule.Contains("--aeterni-spacing-1) / 2", StringComparison.Ordinal)
            && comfortableRule.Contains("--aeterni-radius-lg", StringComparison.Ordinal)
            && comfortableRule.Contains("--aeterni-font-size-sm) - 1px", StringComparison.Ordinal)
            && comfortableRule.Contains("--aeterni-text-primary", StringComparison.Ordinal),
            "NavMenu Comfortable density must align to the workbench sidebar (38px / 3px / 8px / 2px / 13px / primary ink).");

        var compactRule = Rule(css, ".aeterni-nav-menu--density-compact");
        Require(compactRule.Contains("--aeterni-control-height-sm", StringComparison.Ordinal)
            && compactRule.Contains("--aeterni-font-size-xs", StringComparison.Ordinal)
            && compactRule.Contains("--aeterni-text-secondary", StringComparison.Ordinal),
            "NavMenu Compact density must use the 28px control height, 12px label and secondary ink.");

        Require(Rule(css, ".aeterni-nav-menu--indent-compact").Contains("--aeterni-spacing-3", StringComparison.Ordinal)
            && Rule(css, ".aeterni-nav-menu--indent-spacious").Contains("--aeterni-spacing-5", StringComparison.Ordinal),
            "NavMenu indent modifiers must declare their component-level variables.");

        // The removed size tiers must not leave orphan classes or rules behind.
        Require(!css.Contains("aeterni-nav-menu--icon-", StringComparison.Ordinal)
            && !css.Contains("aeterni-nav-menu--description-", StringComparison.Ordinal),
            "NavMenu must not keep icon/description size modifier classes after the properties were removed.");

        var rowHover = Rule(css, ".aeterni-nav-menu__row:hover");
        Require(rowHover.Contains("--aeterni-state-background-hover", StringComparison.Ordinal),
            "NavMenu row hover must paint --aeterni-state-background-hover on the row container.");

        var rowSelected = Rule(css, ".aeterni-nav-menu__row.is-selected");
        Require(rowSelected.Contains("--aeterni-state-background-selected", StringComparison.Ordinal)
            && rowSelected.Contains("--aeterni-state-color-selected", StringComparison.Ordinal)
            && rowSelected.Contains("--aeterni-state-background-checked", StringComparison.Ordinal)
            && rowSelected.Contains("box-shadow", StringComparison.Ordinal),
            "NavMenu selected rows must paint the shared selected/checked state tokens and the inset rail on the row container.");

        Require(css.Contains(":has(> .aeterni-nav-menu__button:active", StringComparison.Ordinal)
            && css.Contains("--aeterni-state-background-active", StringComparison.Ordinal),
            "NavMenu pressed state must be observed from the inner button and painted on the row container.");

        Require(Rule(css, ".aeterni-nav-menu__row.is-disabled").Contains("--aeterni-state-color-disabled", StringComparison.Ordinal)
            && css.Contains(".aeterni-nav-menu__row:hover:not(.is-disabled)", StringComparison.Ordinal),
            "NavMenu disabled rows must keep disabled ink and must not respond to hover.");

        // The old paint lived on the button; the row owns it now, so the button must
        // stay transparent and only carry the focus ring.
        Require(!css.Contains(".aeterni-nav-menu__button:hover", StringComparison.Ordinal)
            && !css.Contains(".aeterni-nav-menu__button.is-selected", StringComparison.Ordinal),
            "NavMenu must not paint hover/selected on the inner button again.");

        // Parameter validation: every remaining option rejects undefined enum values
        // with the parameter name and the actual value.
        foreach (var (parameter, value) in new (string Parameter, object Value)[]
        {
            (nameof(NavMenu.Density), (NavMenuDensity)99),
            (nameof(NavMenu.ChildIndent), (NavMenuChildIndent)99)
        })
        {
            try
            {
                await RenderAsync(new Dictionary<string, object?>
                {
                    ["Items"] = Items(),
                    [parameter] = value
                });
                Require(false, $"NavMenu {parameter} must reject an undefined enum value instead of rendering.");
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Require(exception.ParamName == parameter && Equals(exception.ActualValue, value),
                    $"NavMenu {parameter} errors must identify {parameter} and carry the actual value.");
            }
            catch (Exception exception)
            {
                Require(false,
                    $"NavMenu {parameter} must throw ArgumentOutOfRangeException, got {exception.GetType().Name}.");
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aeterni_ui.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
