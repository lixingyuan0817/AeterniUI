using System.Text.RegularExpressions;
using AeterniUI.Components.Dialog;
using AeterniUI.Components.IconButtonMenu;
using AeterniUI.Components.Menu;
using AeterniUI.Components.MenuButton;
using AeterniUI.Components.Popup;
using AeterniUI.Enums;
using AeterniUI.Models.Dialog;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>
/// Contract checks for the 10.43.0 Menu icon-size/width, Popover min-width/width
/// and Dialog first-field focus option.
/// </summary>
internal static class MenuDialogChecks
{
    private static readonly IReadOnlyList<MenuGroup> Groups =
    [
        new("main", "Main", [
            new MenuItem("edit", "Edit", "Change the current record"),
            new MenuItem("delete", "Delete")
        ])
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

        var root = FindRepositoryRoot();
        var popoverCss = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "AeterniUI", "Components", "Popup", "Popover.razor.css"));
        var menuCss = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "AeterniUI", "Components", "Menu", "Menu.razor.css"));
        var tokenCss = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "AeterniUI", "wwwroot", "css", "aeterni_ui.css"));
        var dialogJs = await File.ReadAllTextAsync(
            Path.Combine(root, "src", "AeterniUI", "Components", "Dialog", "DialogProvider.razor.js"));

        // --- Popover width parameters ------------------------------------------
        var popoverHtml = await RenderAsync<Popover>(new Dictionary<string, object?>
        {
            [nameof(Popover.Id)] = "contract-popover-width",
            [nameof(Popover.Open)] = true,
            [nameof(Popover.AriaLabel)] = "Sized layer",
            [nameof(Popover.Style)] = "margin: 0; --aeterni-contract-keep: kept",
            [nameof(Popover.MinWidth)] = "18rem",
            [nameof(Popover.Width)] = "24rem"
        });

        var popoverRoot = Regex.Match(popoverHtml, "<div\\b[^>]*id=\"contract-popover-width\"[^>]*>").Value;
        Require(popoverRoot.Contains("--aeterni-popover-min-width: 18rem", StringComparison.Ordinal)
            && popoverRoot.Contains("--aeterni-popover-width: 24rem", StringComparison.Ordinal),
            "Popover.MinWidth / Width must land on the root style as the two width custom properties.");
        Require(popoverRoot.Contains("margin: 0", StringComparison.Ordinal)
            && popoverRoot.Contains("--aeterni-contract-keep: kept", StringComparison.Ordinal),
            "The width parameters must append to the host Style instead of replacing its other declarations.");

        var plainPopoverHtml = await RenderAsync<Popover>(new Dictionary<string, object?>
        {
            [nameof(Popover.Id)] = "contract-popover-plain",
            [nameof(Popover.Open)] = true,
            [nameof(Popover.AriaLabel)] = "Plain layer"
        });
        Require(!plainPopoverHtml.Contains("--aeterni-popover-min-width", StringComparison.Ordinal)
            && !plainPopoverHtml.Contains("--aeterni-popover-width", StringComparison.Ordinal),
            "An unset MinWidth / Width must not write the width variables; the surface keeps its token defaults.");

        Require(popoverCss.Contains(
                "min-width: var(--aeterni-popover-min-width, var(--aeterni-overlay-min-width-sm))",
                StringComparison.Ordinal)
            && popoverCss.Contains("width: var(--aeterni-popover-width, auto)", StringComparison.Ordinal),
            "The popover surface must consume both width variables with the documented fallbacks.");
        Require(popoverCss.Contains("--aeterni-overlay-max-width-sm", StringComparison.Ordinal),
            "The width parameters must keep the existing max-width clamp.");

        foreach (var (parameter, what) in new[]
                 {
                     (nameof(Popover.MinWidth), "MinWidth"),
                     (nameof(Popover.Width), "Width")
                 })
        {
            foreach (var blank in new[] { "", "   " })
            {
                try
                {
                    await RenderAsync<Popover>(new Dictionary<string, object?>
                    {
                        [nameof(Popover.Open)] = true,
                        [parameter] = blank
                    });
                    Require(false, $"Popover must reject a blank {what} instead of rendering an empty declaration.");
                }
                catch (ArgumentException exception)
                {
                    Require(exception.GetType() == typeof(ArgumentException) && exception.ParamName == parameter,
                        $"Popover must reject a blank {what} with ArgumentException naming {parameter}.");
                }
            }
        }

        // --- Menu icon size tiers ----------------------------------------------
        Require(!Regex.IsMatch(menuCss, @"aeterni-menu--icon-default"),
            "The default Menu icon size must not emit or style a modifier class.");

        var defaultMenu = await RenderAsync<Menu>(new Dictionary<string, object?>
        {
            [nameof(Menu.Id)] = "contract-menu-icon-default",
            [nameof(Menu.Items)] = Groups
        });
        Require(!defaultMenu.Contains("aeterni-menu--icon-", StringComparison.Ordinal),
            "Menu must not emit an icon-size modifier when IconSize is Default.");

        var smallMenu = await RenderAsync<Menu>(new Dictionary<string, object?>
        {
            [nameof(Menu.Items)] = Groups,
            [nameof(Menu.IconSize)] = MenuIconSize.Small
        });
        var largeMenu = await RenderAsync<Menu>(new Dictionary<string, object?>
        {
            [nameof(Menu.Items)] = Groups,
            [nameof(Menu.IconSize)] = MenuIconSize.Large
        });
        Require(Regex.IsMatch(smallMenu, "<nav[^>]*class=\"[^\"]*aeterni-menu--icon-small[^\"]*\""),
            "Menu.IconSize=Small must mark the root with the small icon modifier.");
        Require(Regex.IsMatch(largeMenu, "<nav[^>]*class=\"[^\"]*aeterni-menu--icon-large[^\"]*\""),
            "Menu.IconSize=Large must mark the root with the large icon modifier.");

        Require(menuCss.Contains("--aeterni-menu-icon-size: var(--aeterni-icon-size-lg)", StringComparison.Ordinal)
            && menuCss.Contains(".aeterni-menu--icon-small", StringComparison.Ordinal)
            && menuCss.Contains("--aeterni-menu-icon-size: var(--aeterni-icon-size-md)", StringComparison.Ordinal)
            && menuCss.Contains(".aeterni-menu--icon-large", StringComparison.Ordinal)
            && menuCss.Contains("--aeterni-menu-icon-size: var(--aeterni-icon-size-xl)", StringComparison.Ordinal),
            "Menu must declare the 20px default and the 16px / 24px icon-size tiers through --aeterni-menu-icon-size.");
        Require(Regex.IsMatch(menuCss, @"\.aeterni-menu__icon\s*\{[^}]*width:\s*var\(--aeterni-menu-icon-size\)", RegexOptions.Singleline)
            && Regex.IsMatch(menuCss, @"\.aeterni-menu__icon\s*\{[^}]*height:\s*var\(--aeterni-menu-icon-size\)", RegexOptions.Singleline),
            "The icon slot must consume --aeterni-menu-icon-size for both width and height, so the glyph follows the box.");
        Require(Regex.IsMatch(menuCss, @"\.aeterni-menu__chevron\s*\{[^}]*--aeterni-icon-render-size:\s*var\(--aeterni-icon-size-sm\)", RegexOptions.Singleline),
            "The disclosure chevron must keep its own small icon size.");

        try
        {
            await RenderAsync<Menu>(new Dictionary<string, object?>
            {
                [nameof(Menu.Items)] = Groups,
                [nameof(Menu.IconSize)] = (MenuIconSize)99
            });
            Require(false, "Menu must reject an undefined IconSize instead of rendering.");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Require(exception.ParamName == nameof(Menu.IconSize) && Equals(exception.ActualValue, (MenuIconSize)99),
                "Menu must reject an undefined IconSize with ArgumentOutOfRangeException carrying the actual value.");
        }

        // --- Menu flyouts opt into the narrow overlay minimum -------------------
        RenderFragment icon = builder =>
        {
            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "viewBox", "0 0 16 16");
            builder.CloseElement();
        };

        var menuButtonHtml = await RenderAsync<MenuButton>(new Dictionary<string, object?>
        {
            [nameof(MenuButton.Label)] = "Actions",
            [nameof(MenuButton.Items)] = Groups
        });
        var iconButtonMenuHtml = await RenderAsync<IconButtonMenu>(new Dictionary<string, object?>
        {
            [nameof(IconButtonMenu.Icon)] = icon,
            [nameof(IconButtonMenu.Items)] = Groups,
            [nameof(IconButtonMenu.AriaLabel)] = "Actions"
        });
        const string narrowWidth = "--aeterni-popover-min-width: var(--aeterni-overlay-min-width-xs)";
        Require(menuButtonHtml.Contains(narrowWidth, StringComparison.Ordinal)
            && iconButtonMenuHtml.Contains(narrowWidth, StringComparison.Ordinal),
            "MenuButton and IconButtonMenu must default their flyout to the narrow --aeterni-overlay-min-width-xs token.");
        Require(tokenCss.Contains("--aeterni-overlay-min-width-xs: 10rem", StringComparison.Ordinal),
            "The token file must declare the narrow overlay minimum next to the existing sm token.");
        Require(Regex.IsMatch(tokenCss, @"--aeterni-overlay-min-width-sm:\s*12rem;[^}]*--aeterni-overlay-min-width-xs:\s*10rem;", RegexOptions.Singleline)
            || Regex.IsMatch(tokenCss, @"--aeterni-overlay-min-width-xs:\s*10rem;[^}]*--aeterni-overlay-min-width-sm:\s*12rem;", RegexOptions.Singleline),
            "The xs overlay minimum must live in the same token family as the sm minimum.");

        // --- Dialog first-field focus option -----------------------------------
        Require(!new DialogOptions().AutofocusFirstField,
            "DialogOptions.AutofocusFirstField must default to false so content dialogs keep their old focus target.");
        Require(new FormDialogOptions().AutofocusFirstField,
            "FormDialogOptions must turn the inherited AutofocusFirstField on from its constructor.");

        Require(dialogJs.Contains("[data-aeterni-autofocus=\"true\"]", StringComparison.Ordinal),
            "The dialog module must focus the first field of any body that carries the autofocus marker, not only form bodies.");

        async Task<string> RenderProviderHtmlAsync(Action<IDialogService> arrange)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IJSRuntime, NoopJsRuntime>();
            services.AddAeterniUI();

            await using var provider = services.BuildServiceProvider();
            arrange(provider.GetRequiredService<IDialogService>());

            await using var providerRenderer = new HtmlRenderer(
                provider,
                provider.GetRequiredService<ILoggerFactory>());

            return await providerRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await providerRenderer.RenderComponentAsync<DialogProvider>(ParameterView.Empty);
                return output.ToHtmlString();
            });
        }

        RenderFragment content = builder => builder.AddContent(0, "custom-dialog-body");

        var focusedDialogHtml = await RenderProviderHtmlAsync(dialogs =>
            _ = dialogs.Show(content, new DialogOptions { Title = "Rename", AutofocusFirstField = true }));
        Require(focusedDialogHtml.Contains("data-aeterni-autofocus=\"true\"", StringComparison.Ordinal)
            && !focusedDialogHtml.Contains("data-aeterni-form-dialog", StringComparison.Ordinal),
            "A custom Dialog with AutofocusFirstField=true must render the autofocus marker without gaining the form markers.");

        var unfocusedDialogHtml = await RenderProviderHtmlAsync(dialogs =>
            _ = dialogs.Show(content, new DialogOptions { Title = "Rename" }));
        Require(!unfocusedDialogHtml.Contains("data-aeterni-autofocus", StringComparison.Ordinal),
            "A custom Dialog that did not opt in must not render the autofocus marker.");

        var formDialogHtml = await RenderProviderHtmlAsync(dialogs =>
            _ = dialogs.ShowForm(content, new FormDialogOptions()));
        Require(formDialogHtml.Contains("data-aeterni-form-dialog=\"true\"", StringComparison.Ordinal)
            && formDialogHtml.Contains("data-aeterni-autofocus=\"true\"", StringComparison.Ordinal),
            "A form dialog must keep its form marker and default autofocus marker.");

        var noAutofocusFormHtml = await RenderProviderHtmlAsync(dialogs =>
            _ = dialogs.ShowForm(content, new FormDialogOptions { AutofocusFirstField = false }));
        Require(noAutofocusFormHtml.Contains("data-aeterni-autofocus=\"false\"", StringComparison.Ordinal),
            "A form dialog must keep stating the autofocus marker as false when the option is turned off.");
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
