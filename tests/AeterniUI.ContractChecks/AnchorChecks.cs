using System.Reflection;
using System.Text.RegularExpressions;
using AeterniUI.Attributes;
using AeterniUI.Components.Anchor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>
/// Contract checks for <c>Anchor</c> (10.38.0): the nav / ol / a shape, the
/// one-to-one link-to-section mapping, the read-only current marker received from
/// the browser callback, and the parameter validation the public API promises.
/// </summary>
internal static class AnchorChecks
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
                var output = await renderer.RenderComponentAsync<Anchor>(ParameterView.FromDictionary(parameters));
                return output.ToHtmlString();
            });
        }

        // The module name/path is the contract the base class loads; interactive
        // init is what creates the instance `attach` later binds to.
        var module = (JsModuleAttribute)Attribute.GetCustomAttribute(typeof(Anchor), typeof(JsModuleAttribute))!;
        Require(module.Interactive && module.Name == "anchor" && module.Path == "Components/Anchor/Anchor.razor.js",
            "Anchor must declare the interactive anchor module.");
        Require(typeof(Anchor).GetMethod(nameof(Anchor.OnActiveSectionChangedAsync))?.GetCustomAttribute<JSInvokableAttribute>() is not null,
            "Anchor must expose the current-section callback to the browser as [JSInvokable].");

        // Defaults are the documented simplest usage: document scrolling, no
        // offset, smooth scrolling and fragment replacement all enabled.
        var defaults = new Anchor();
        Require(defaults.ContainerId is null && defaults.Offset == 0 && defaults.UpdateLocation && defaults.Smooth
            && defaults.AriaLabel is null,
            "Anchor defaults must be document scrolling, offset 0, Smooth=true and UpdateLocation=true.");

        var html = await RenderAsync(new Dictionary<string, object?>
        {
            ["Id"] = "contract-anchor",
            ["Class"] = "consumer-anchor",
            ["Style"] = "margin: 0",
            ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "anchor" },
            ["Items"] = AnchorFixtures.Sections,
            ["AriaLabel"] = "Construction sections"
        });

        var root = Regex.Match(html, "<nav\\b[^>]*>").Value;
        Require(root.Contains("<nav", StringComparison.Ordinal)
            && root.Contains("id=\"contract-anchor\"", StringComparison.Ordinal)
            && root.Contains("consumer-anchor", StringComparison.Ordinal)
            && root.Contains("margin: 0", StringComparison.Ordinal)
            && root.Contains("data-contract=\"anchor\"", StringComparison.Ordinal)
            && root.Contains("aeterni-anchor", StringComparison.Ordinal),
            "Anchor root must be the nav element and preserve the base DOM contract (id, class, style, unmatched attributes).");
        Require(root.Contains("aria-label=\"Construction sections\"", StringComparison.Ordinal),
            "Anchor must render its accessible name on the navigation landmark.");
        Require(html.Contains("<ol class=\"aeterni-anchor__list\"", StringComparison.Ordinal)
            && Regex.Matches(html, "<li class=\"aeterni-anchor__item\"", RegexOptions.None).Count == AnchorFixtures.Sections.Count,
            "Anchor must render an ordered list with one item per AnchorLink.");
        Require(Regex.Matches(html, "<a class=\"aeterni-anchor__link\"", RegexOptions.None).Count == AnchorFixtures.Sections.Count,
            "Anchor must render every link as an anchor with the shared link class.");

        foreach (var item in AnchorFixtures.Sections)
        {
            Require(html.Contains($"href=\"#{item.Id}\"", StringComparison.Ordinal),
                $"Anchor link for section {item.Id} must keep the native fragment href.");
            Require(html.Contains($">{item.Title}</a>", StringComparison.Ordinal),
                $"Anchor must render the link title of section {item.Id}.");
        }

        // The current marker is derived from the browser, not from markup: a static
        // render must not claim a current section.
        Require(!html.Contains("aria-current", StringComparison.Ordinal),
            "A freshly rendered Anchor must not claim a current section.");
        Require(!html.Contains("is-active", StringComparison.Ordinal),
            "A freshly rendered Anchor must not mark any link as active.");

        var defaultLabel = await RenderAsync(new Dictionary<string, object?>
        {
            ["Items"] = AnchorFixtures.Sections
        });
        Require(defaultLabel.Contains("aria-label=\"Page sections\"", StringComparison.Ordinal),
            "Anchor must fall back to the text table for its accessible name.");
        Require(!Regex.IsMatch(defaultLabel, ">[^<]*[\\u4e00-\\u9fff][^<]*<"),
            "Anchor must not render CJK text from built-in defaults.");
        Require(!defaultLabel.Contains("}", StringComparison.Ordinal),
            "Anchor must not leak Razor closing braces into rendered markup.");

        // ContainerId / Offset / UpdateLocation / Smooth are JS-side scroll options,
        // so rendering them must stay markup-neutral beyond the configured root.
        var configured = await RenderAsync(new Dictionary<string, object?>
        {
            ["Items"] = AnchorFixtures.Sections,
            ["ContainerId"] = "contract-anchor-scroller",
            ["Offset"] = 96,
            ["UpdateLocation"] = false,
            ["Smooth"] = false
        });
        Require(configured.Contains("aeterni-anchor", StringComparison.Ordinal)
            && !configured.Contains("aria-current", StringComparison.Ordinal),
            "Container/offset/scroll options must not change the rendered link contract.");
        var negativeOffset = await RenderAsync(new Dictionary<string, object?>
        {
            ["Items"] = AnchorFixtures.Sections,
            ["Offset"] = -12
        });
        Require(negativeOffset.Contains("aeterni-anchor__list", StringComparison.Ordinal),
            "A negative Offset must be tolerated instead of breaking rendering.");

        // The current marker is written by the browser callback. Drive it on a live
        // instance and re-read the markup, as the Cascader/List checks do.
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            AnchorContractHost host = null!;
            var live = await renderer.RenderComponentAsync<AnchorContractHost>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Ready"] = (Action<AnchorContractHost>)(value => host = value) }));

            string NavLabel() => Regex.Match(live.ToHtmlString(), "<nav\\b[^>]*>").Value;
            int CurrentCount() => Regex.Matches(live.ToHtmlString(), "aria-current=\"location\"", RegexOptions.None).Count;
            int ActiveClassCount() => Regex.Matches(live.ToHtmlString(), "is-active", RegexOptions.None).Count;
            string ActiveItem()
            {
                var items = Regex.Matches(live.ToHtmlString(), "<li\\b[^>]*>[\\s\\S]*?</li>", RegexOptions.None);
                return items.Select(match => match.Value)
                    .FirstOrDefault(value => value.Contains("aria-current", StringComparison.Ordinal)) ?? string.Empty;
            }

            string LinkMarkup(string id) =>
                Regex.Match(live.ToHtmlString(), $"<a\\b[^>]*href=\"#{id}\"[^>]*>").Value;

            Require(NavLabel().Contains("aria-label=\"Contract sections\"", StringComparison.Ordinal),
                "A host-supplied AriaLabel must replace the text-table default.");
            Require(CurrentCount() == 0 && ActiveClassCount() == 0,
                "An Anchor that has not been observed yet must not mark a current section.");

            await host.Inner.OnActiveSectionChangedAsync("install");
            host.Update();
            Require(CurrentCount() == 1 && ActiveClassCount() == 1,
                "The browser callback must produce exactly one current section.");
            Require(ActiveItem().Contains("href=\"#install\"", StringComparison.Ordinal),
                "aria-current=\"location\" must land on the link of the reported section.");
            Require(!LinkMarkup("intro").Contains("aria-current", StringComparison.Ordinal)
                && !LinkMarkup("usage").Contains("aria-current", StringComparison.Ordinal),
                "The other links must not carry the current marker.");

            // Repeating the same section is not a state change; reporting an id the
            // instance does not render (or null) clears the marker.
            await host.Inner.OnActiveSectionChangedAsync("install");
            await host.Inner.OnActiveSectionChangedAsync("missing");
            host.Update();
            Require(CurrentCount() == 0 && ActiveClassCount() == 0,
                "An unknown section id must clear the current marker instead of pointing aria-current at nothing.");

            await host.Inner.OnActiveSectionChangedAsync("intro");
            host.Update();
            Require(CurrentCount() == 1 && ActiveItem().Contains("href=\"#intro\"", StringComparison.Ordinal),
                "The first section can become current like any other reported section.");

            await host.Inner.OnActiveSectionChangedAsync(null);
            host.Update();
            Require(CurrentCount() == 0 && ActiveClassCount() == 0,
                "A null section id must clear the current marker.");
        });

        async Task RequireInvalidItemsAsync(object? items, Type expectedException)
        {
            try
            {
                await RenderAsync(new Dictionary<string, object?> { ["Items"] = items });
                Require(false, $"Anchor must reject invalid Items ({items?.GetType().Name ?? "null"}).");
            }
            catch (ArgumentException exception)
            {
                Require(exception.GetType() == expectedException && exception.ParamName == "Items",
                    $"Anchor invalid item errors must identify Items with {expectedException.Name}.");
            }
        }

        await RequireInvalidItemsAsync(null, typeof(ArgumentNullException));
        await RequireInvalidItemsAsync(new AnchorLink?[] { null }, typeof(ArgumentException));
        await RequireInvalidItemsAsync(new[] { new AnchorLink(" ", "Introduction") }, typeof(ArgumentException));
        await RequireInvalidItemsAsync(
            new[] { new AnchorLink("intro", "Introduction"), new AnchorLink("intro", "Again") },
            typeof(ArgumentException));
        await RequireInvalidItemsAsync(new[] { new AnchorLink("intro", " ") }, typeof(ArgumentException));
    }
}
