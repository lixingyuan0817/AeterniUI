using System.Text.RegularExpressions;
using AeterniUI.Enums;
using AeterniUI.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ButtonComponent = AeterniUI.Components.Button.Button;
using IconButtonComponent = AeterniUI.Components.IconButton.IconButton;
using IconComponent = AeterniUI.Components.Icon.Icon;

/// <summary>
/// Contract checks for the 10.42.0 chrome controls: <c>IconButton</c> shape/tone/compact,
/// <c>Button</c> content alignment and <c>Icon</c> tone.
/// </summary>
/// <remarks>
/// The three additions exist to replace the workbench's <c>::deep</c> overrides of
/// the built-in chrome, so the checks pin the things that override relied on: the
/// emitted class (and therefore its CSS consumer), the documented defaults that
/// keep existing consumers untouched, and the state contracts (loading, disabled,
/// focus) that a chrome preset must not disturb.
/// </remarks>
internal static class ChromeControlsChecks
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

        async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters)
            where TComponent : IComponent
        {
            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
                return output.ToHtmlString();
            });
        }

        async Task RequireEnumRejectedAsync<TComponent, TEnum>(string parameter, TEnum invalid)
            where TComponent : IComponent
            where TEnum : struct, Enum
        {
            try
            {
                await RenderAsync<TComponent>(new Dictionary<string, object?> { [parameter] = invalid });
                Require(false, $"{typeof(TComponent).Name} must reject the undefined {parameter} {invalid}.");
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Require(exception.ParamName == parameter && Equals(exception.ActualValue, invalid),
                    $"{typeof(TComponent).Name} must reject an undefined {parameter} with ArgumentOutOfRangeException carrying the actual value.");
            }
            catch (Exception exception)
            {
                Require(false, $"{typeof(TComponent).Name} must reject an undefined {parameter} with ArgumentOutOfRangeException, got {exception.GetType().Name}.");
            }
        }

        var repositoryRoot = FindRepositoryRoot();
        var components = Path.Combine(repositoryRoot, "src", "AeterniUI", "Components");
        var iconButtonCss = await File.ReadAllTextAsync(Path.Combine(components, "IconButton", "IconButton.razor.css"));
        var buttonCss = await File.ReadAllTextAsync(Path.Combine(components, "Button", "Button.razor.css"));
        var iconCss = await File.ReadAllTextAsync(Path.Combine(components, "Icon", "Icon.razor.css"));

        RenderFragment glyph = builder =>
        {
            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "viewBox", "0 0 16 16");
            builder.CloseElement();
        };

        // --- defaults -------------------------------------------------------
        // Defaults are the documented simplest usage: the chrome additions stay
        // silent so existing consumers render exactly as before.
        var iconButtonDefaults = new IconButtonComponent();
        Require(iconButtonDefaults.Shape == IconButtonShape.Circle
            && iconButtonDefaults.Tone == IconButtonTone.Default
            && !iconButtonDefaults.Compact,
            "IconButton must default to Shape=Circle, Tone=Default and Compact=false.");

        var buttonDefaults = new ButtonComponent();
        Require(buttonDefaults.ContentAlignment == ButtonContentAlignment.Center,
            "Button must default to ContentAlignment=Center.");

        var iconDefaults = new IconComponent();
        Require(iconDefaults.Tone == IconTone.Default,
            "Icon must default to Tone=Default.");

        // --- IconButton: default rendering stays untouched -------------------
        var defaultIconButton = await RenderAsync<IconButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(IconButtonComponent.AriaLabel)] = "Search"
        });
        Require(defaultIconButton.Contains("aeterni-icon-button--ghost", StringComparison.Ordinal)
            && defaultIconButton.Contains("aeterni-icon-button--neutral", StringComparison.Ordinal)
            && !defaultIconButton.Contains("aeterni-icon-button--square", StringComparison.Ordinal)
            && !defaultIconButton.Contains("aeterni-icon-button--circle", StringComparison.Ordinal)
            && !defaultIconButton.Contains("--tone-", StringComparison.Ordinal)
            && !defaultIconButton.Contains("is-compact", StringComparison.Ordinal),
            "The default IconButton must be the round Ghost + Neutral chrome and emit no shape modifier.");
        Require(Regex.IsMatch(iconButtonCss, @"\.aeterni-icon-button\s*\{[^}]*border-radius: var\(--aeterni-radius-full\)"),
            "The round default must live in the base rule so it needs no modifier class.");

        // --- IconButton.Shape ------------------------------------------------
        var square = await RenderAsync<IconButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(IconButtonComponent.AriaLabel)] = "Window menu",
            [nameof(IconButtonComponent.Shape)] = IconButtonShape.Square,
            [nameof(IconButtonComponent.Icon)] = glyph
        });
        Require(square.Contains("aeterni-icon-button--square", StringComparison.Ordinal),
            "IconButton.Shape=Square must emit the square modifier.");
        Require(Regex.IsMatch(iconButtonCss, @"\.aeterni-icon-button\.aeterni-icon-button--square\s*\{[^}]*border-radius: var\(--aeterni-radius-button\)"),
            "The square modifier must consume --aeterni-radius-button instead of a literal radius.");

        // --- IconButton.Tone=Muted ------------------------------------------
        var muted = await RenderAsync<IconButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(IconButtonComponent.AriaLabel)] = "More",
            [nameof(IconButtonComponent.Tone)] = IconButtonTone.Muted,
            [nameof(IconButtonComponent.Compact)] = true,
            [nameof(IconButtonComponent.Size)] = Size.Large,
            [nameof(IconButtonComponent.Icon)] = glyph
        });
        Require(muted.Contains("aeterni-icon-button--tone-muted", StringComparison.Ordinal)
            && muted.Contains("is-compact", StringComparison.Ordinal)
            // Compact overrides the size geometry but does not remove the Size
            // contract; the CSS compound rule is what wins.
            && muted.Contains("aeterni-icon-button--lg", StringComparison.Ordinal),
            "A muted compact IconButton must emit the tone, compact and size classes.");

        Require(Regex.IsMatch(iconButtonCss,
                @"\.aeterni-icon-button\.aeterni-icon-button--tone-muted\s*\{[^}]*border-color: transparent;[^}]*background: transparent;[^}]*color: var\(--aeterni-text-tertiary\)"),
            "The muted tone must paint a transparent surface with tertiary ink.");
        Require(Regex.IsMatch(iconButtonCss,
                @"\.aeterni-icon-button\.aeterni-icon-button--tone-muted:hover:not\(:disabled\)\s*\{[^}]*background: var\(--aeterni-state-background-hover\);[^}]*color: var\(--aeterni-text-secondary\);"),
            "The muted tone hover must step the shared hover surface and lift its ink, not paint a variant colour.");
        Require(Regex.IsMatch(iconButtonCss,
                @"\.aeterni-icon-button\.aeterni-icon-button--tone-muted:active:not\(:disabled\)\s*\{[^}]*background: var\(--aeterni-state-background-active\);"),
            "The muted tone active state must step the shared active surface.");
        Require(iconButtonCss.Contains("--aeterni-state-background-hover", StringComparison.Ordinal)
            && iconButtonCss.Contains("--aeterni-state-background-active", StringComparison.Ordinal),
            "The muted tone must consume the shared state surface tokens.");

        // The whole series answers hover in place: no lift and no elevation
        // shadow, while the glyph ink carries the highlight. The old base rules
        // translated -1px and painted --aeterni-shadow-sm, which forced every
        // chrome tone to reset both; the file-level assertions stop either from
        // creeping back through a single variant.
        Require(!iconButtonCss.Contains("transform", StringComparison.Ordinal),
            "IconButton hover must stay in place; the component CSS may not declare a transform.");
        Require(!iconButtonCss.Contains("--aeterni-shadow-sm", StringComparison.Ordinal),
            "IconButton hover must not paint the shared elevation shadow.");
        var baseHover = Regex.Match(iconButtonCss,
            @"\.aeterni-icon-button:hover:not\(:disabled\)\s*\{([^}]*)\}").Groups[1].Value;
        Require(baseHover.Length > 0
            && !baseHover.Contains("box-shadow", StringComparison.Ordinal),
            "The filled IconButton hover may only step its surface, not add depth.");

        // Hover feedback is an ink step: the transparent variants mix the intent
        // ink toward the theme primary ink, which deepens it on the light theme
        // and brightens it on the dark one. Neutral already sits at primary ink,
        // so the in-place surface step carries the feedback there.
        Require(Regex.IsMatch(iconButtonCss,
                @"--aeterni-icon-button-hover-foreground:\s*color-mix\(in srgb, var\(--aeterni-button-color-hover\) 72%, var\(--aeterni-text-primary\)\)"),
            "Transparent IconButton variants must derive their hover ink from the intent ink and the theme ink.");
        var transparentHover = Regex.Match(iconButtonCss,
            @"\.aeterni-icon-button--outline:hover:not\(:disabled\),[^{]*\.aeterni-icon-button--link:hover:not\(:disabled\)\s*\{([^}]*)\}").Groups[1].Value;
        Require(transparentHover.Contains("color: var(--aeterni-icon-button-hover-foreground)", StringComparison.Ordinal),
            "Transparent IconButton hover must paint the derived hover ink instead of moving the box.");

        // --- IconButton.Tone=DangerTinted ------------------------------------
        var dangerTinted = await RenderAsync<IconButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(IconButtonComponent.AriaLabel)] = "Close window",
            [nameof(IconButtonComponent.Tone)] = IconButtonTone.DangerTinted,
            [nameof(IconButtonComponent.Shape)] = IconButtonShape.Circle,
            [nameof(IconButtonComponent.Icon)] = glyph
        });
        Require(dangerTinted.Contains("aeterni-icon-button--tone-danger-tinted", StringComparison.Ordinal),
            "IconButton.Tone=DangerTinted must emit its own tint modifier, not the danger intent class.");
        Require(!dangerTinted.Contains("aeterni-icon-button--danger ", StringComparison.Ordinal),
            "The tinted tone must not also wear the solid danger intent modifier.");

        // The tint is computed from the one semantic danger colour: no second
        // colour variable, and the ink uses the text stop, not the fill stop.
        Require(Regex.IsMatch(iconButtonCss,
                @"\.aeterni-icon-button\.aeterni-icon-button--tone-danger-tinted\s*\{[^}]*background: color-mix\(in srgb, var\(--aeterni-color-danger-default\) 12%, transparent\);[^}]*color: var\(--aeterni-color-danger-text\)"),
            "The tinted tone must paint a 12% danger surface with the danger text ink.");
        Require(iconButtonCss.Contains("color-mix(in srgb, var(--aeterni-color-danger-default) 25%, transparent)", StringComparison.Ordinal)
            && iconButtonCss.Contains("color-mix(in srgb, var(--aeterni-color-danger-default) 20%, transparent)", StringComparison.Ordinal)
            && iconButtonCss.Contains("color-mix(in srgb, var(--aeterni-color-danger-default) 55%, transparent)", StringComparison.Ordinal)
            && iconButtonCss.Contains("color-mix(in srgb, var(--aeterni-color-danger-default) 26%, transparent)", StringComparison.Ordinal),
            "The tinted tone must compute its border (25%), hover fill (20%), hover border (55%) and active fill (26%) from the danger colour.");
        Require(iconButtonCss.Contains("0 0 0 3px color-mix(in srgb, var(--aeterni-color-danger-default) 16%, transparent)", StringComparison.Ordinal),
            "The tinted tone hover must draw the 3px 16% danger halo.");
        Require(!Regex.IsMatch(iconButtonCss, @"--aeterni-color-danger-(tint|surface|border)\s*:"),
            "The tinted tone must not create a second danger colour variable set.");
        Require(!Regex.IsMatch(iconButtonCss, @"\.aeterni-icon-button--tone-danger-tinted[^\{]*\{[^}]*outline", RegexOptions.Singleline),
            "A chrome tone must not repaint the focus ring.");

        // --- IconButton.Compact ---------------------------------------------
        Require(Regex.IsMatch(iconButtonCss,
                @"\.aeterni-icon-button\.is-compact\s*\{[^}]*--aeterni-icon-button-size: calc\(var\(--aeterni-control-height-sm\) - var\(--aeterni-spacing-1\)\);[^}]*--aeterni-icon-button-icon-size: var\(--aeterni-icon-size-sm\)"),
            "The compact tier must derive its 24px box and 14px glyph from the shared height, spacing and icon tokens.");
        // Only geometry may move: padding, focus ring, loading sizing and the
        // ButtonGroup divider variables stay as declared by the base rules.
        var compactBlock = Regex.Match(iconButtonCss, @"\.aeterni-icon-button\.is-compact\s*\{([^}]*)\}").Groups[1].Value;
        Require(!compactBlock.Contains("padding", StringComparison.Ordinal)
            && !compactBlock.Contains("outline", StringComparison.Ordinal)
            && !compactBlock.Contains("--aeterni-button-divider-", StringComparison.Ordinal)
            && !compactBlock.Contains("loading", StringComparison.Ordinal),
            "The compact tier may only move the box and glyph geometry.");

        // --- IconButton state contract is untouched --------------------------
        var loading = await RenderAsync<IconButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(IconButtonComponent.AriaLabel)] = "Saving",
            [nameof(IconButtonComponent.Loading)] = true,
            [nameof(IconButtonComponent.Tone)] = IconButtonTone.Muted,
            [nameof(IconButtonComponent.Compact)] = true,
            [nameof(IconButtonComponent.Icon)] = glyph
        });
        var loadingRoot = Regex.Match(loading, "<button\\b[^>]*>").Value;
        Require(loadingRoot.Contains("disabled", StringComparison.Ordinal)
            && loadingRoot.Contains("aria-disabled=\"true\"", StringComparison.Ordinal)
            && loadingRoot.Contains("aria-busy=\"true\"", StringComparison.Ordinal)
            && loadingRoot.Contains("is-loading", StringComparison.Ordinal)
            && loading.Contains("aeterni-icon-button__loading", StringComparison.Ordinal),
            "A loading chrome IconButton must keep the disabled/busy contract and the busy glyph.");
        Require(iconButtonCss.Contains(":disabled:not(.is-loading)", StringComparison.Ordinal)
            && iconButtonCss.Contains("--aeterni-icon-button-loading-foreground", StringComparison.Ordinal),
            "The chrome options must not disturb the loading-vs-disabled ink split.");

        var disabled = await RenderAsync<IconButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(IconButtonComponent.AriaLabel)] = "Unavailable",
            [nameof(IconButtonComponent.Disabled)] = true,
            [nameof(IconButtonComponent.Tone)] = IconButtonTone.DangerTinted,
            [nameof(IconButtonComponent.Compact)] = true
        });
        var disabledRoot = Regex.Match(disabled, "<button\\b[^>]*>").Value;
        Require(disabledRoot.Contains("disabled", StringComparison.Ordinal)
            && disabledRoot.Contains("aria-disabled=\"true\"", StringComparison.Ordinal)
            && !disabledRoot.Contains("aria-busy", StringComparison.Ordinal)
            && !disabledRoot.Contains("is-loading", StringComparison.Ordinal),
            "A disabled chrome IconButton must keep the native disabled contract without a busy state.");

        // Focus is the shared focus ring: the chrome additions must not replace it.
        Require(Regex.IsMatch(iconButtonCss,
                @"\.aeterni-icon-button:focus-visible\s*\{[^}]*outline: var\(--aeterni-focus-width\) solid var\(--aeterni-focus-color\)"),
            "The chrome IconButton must keep the shared focus-visible ring.");

        // --- Button.ContentAlignment ----------------------------------------
        var defaultButton = await RenderAsync<ButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(ButtonComponent.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Save"))
        });
        Require(!defaultButton.Contains("aeterni-button--content-start", StringComparison.Ordinal),
            "A Button without ContentAlignment must not emit the start-alignment modifier.");

        var startButton = await RenderAsync<ButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(ButtonComponent.ContentAlignment)] = ButtonContentAlignment.Start,
            [nameof(ButtonComponent.FullWidth)] = true,
            [nameof(ButtonComponent.StartIcon)] = glyph,
            [nameof(ButtonComponent.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "New session"))
        });
        Require(startButton.Contains("aeterni-button--content-start", StringComparison.Ordinal)
            && startButton.Contains("aeterni-button__content", StringComparison.Ordinal)
            && startButton.Contains("aeterni-button__icon", StringComparison.Ordinal)
            && startButton.Contains("New session", StringComparison.Ordinal),
            "ContentAlignment=Start must keep the single content row with icon and label.");
        Require(startButton.Contains("is-full-width", StringComparison.Ordinal),
            "ContentAlignment=Start must leave the width contract to FullWidth.");
        Require(Regex.IsMatch(buttonCss,
                @"\.aeterni-button\.aeterni-button--content-start \.aeterni-button__content\s*\{[^}]*justify-content: flex-start"),
            "The start-alignment modifier must re-lay the content row, not the button itself.");
        Require(buttonCss.Contains("flex: 1 1 auto", StringComparison.Ordinal),
            "The start-aligned content row must fill the button width.");

        var centeredFullWidth = await RenderAsync<ButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(ButtonComponent.FullWidth)] = true,
            [nameof(ButtonComponent.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Save"))
        });
        Require(centeredFullWidth.Contains("is-full-width", StringComparison.Ordinal)
            && !centeredFullWidth.Contains("aeterni-button--content-start", StringComparison.Ordinal),
            "FullWidth alone must keep the centred content contract.");

        var startLoading = await RenderAsync<ButtonComponent>(new Dictionary<string, object?>
        {
            [nameof(ButtonComponent.ContentAlignment)] = ButtonContentAlignment.Start,
            [nameof(ButtonComponent.Loading)] = true,
            [nameof(ButtonComponent.FullWidth)] = true,
            [nameof(ButtonComponent.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "Saving"))
        });
        Require(startLoading.Contains("aeterni-button__loading", StringComparison.Ordinal)
            && startLoading.Contains("is-loading", StringComparison.Ordinal)
            && startLoading.Contains("aria-busy=\"true\"", StringComparison.Ordinal),
            "ContentAlignment=Start must not change the loading contract.");

        // --- Icon.Tone -------------------------------------------------------
        var defaultIcon = await RenderAsync<IconComponent>(new Dictionary<string, object?>
        {
            [nameof(IconComponent.Definition)] = AeterniIcons.Star
        });
        Require(defaultIcon.Contains("aeterni-icon", StringComparison.Ordinal)
            && !defaultIcon.Contains("--tone-", StringComparison.Ordinal)
            && defaultIcon.Contains("fill=\"currentColor\"", StringComparison.Ordinal)
            && defaultIcon.Contains("aria-hidden=\"true\"", StringComparison.Ordinal),
            "An Icon without Tone must keep currentColor and the decorative default.");

        var mutedIcon = await RenderAsync<IconComponent>(new Dictionary<string, object?>
        {
            [nameof(IconComponent.Definition)] = AeterniIcons.Star,
            [nameof(IconComponent.Tone)] = IconTone.Muted
        });
        var tertiaryIcon = await RenderAsync<IconComponent>(new Dictionary<string, object?>
        {
            [nameof(IconComponent.Definition)] = AeterniIcons.Star,
            [nameof(IconComponent.Tone)] = IconTone.Tertiary
        });
        Require(mutedIcon.Contains("aeterni-icon--tone-muted", StringComparison.Ordinal)
            && tertiaryIcon.Contains("aeterni-icon--tone-tertiary", StringComparison.Ordinal),
            "Icon.Tone must emit its ink-tier modifier.");
        Require(Regex.IsMatch(iconCss, @"\.aeterni-icon--tone-muted\s*\{[^}]*color: var\(--aeterni-text-secondary\)")
            && Regex.IsMatch(iconCss, @"\.aeterni-icon--tone-tertiary\s*\{[^}]*color: var\(--aeterni-text-tertiary\)"),
            "The icon ink tiers must consume the shared secondary/tertiary text tokens.");

        // Color carries semantic colour and stays untouched by the new axis.
        var semanticIcon = await RenderAsync<IconComponent>(new Dictionary<string, object?>
        {
            [nameof(IconComponent.Definition)] = AeterniIcons.Star,
            [nameof(IconComponent.Color)] = Color.Warning
        });
        Require(semanticIcon.Contains("aeterni-icon--warning", StringComparison.Ordinal)
            && !semanticIcon.Contains("--tone-", StringComparison.Ordinal)
            && iconCss.Contains("color: var(--aeterni-color-warning-text)", StringComparison.Ordinal),
            "Icon.Color must keep its semantic colour pipeline and not become a tone.");

        var bothAxes = await RenderAsync<IconComponent>(new Dictionary<string, object?>
        {
            [nameof(IconComponent.Definition)] = AeterniIcons.Star,
            [nameof(IconComponent.Color)] = Color.Danger,
            [nameof(IconComponent.Tone)] = IconTone.Tertiary
        });
        Require(bothAxes.Contains("aeterni-icon--danger", StringComparison.Ordinal)
            && bothAxes.Contains("aeterni-icon--tone-tertiary", StringComparison.Ordinal),
            "Color and Tone are independent axes and must render together.");
        // The tone is declared after the colour rules, so it wins when a caller
        // sets both axes; that ordering is the documented precedence.
        Require(iconCss.IndexOf(".aeterni-icon--tone-tertiary", StringComparison.Ordinal)
            > iconCss.IndexOf(".aeterni-icon--danger", StringComparison.Ordinal),
            "The icon tone rules must be declared after the colour rules so the ink tier is the deciding layer.");

        // --- enum validation -------------------------------------------------
        await RequireEnumRejectedAsync<IconButtonComponent, IconButtonShape>(
            nameof(IconButtonComponent.Shape), (IconButtonShape)99);
        await RequireEnumRejectedAsync<IconButtonComponent, IconButtonTone>(
            nameof(IconButtonComponent.Tone), (IconButtonTone)99);
        await RequireEnumRejectedAsync<ButtonComponent, ButtonContentAlignment>(
            nameof(ButtonComponent.ContentAlignment), (ButtonContentAlignment)99);
        await RequireEnumRejectedAsync<IconComponent, IconTone>(
            nameof(IconComponent.Tone), (IconTone)99);

        // --- every new class has a consumer ---------------------------------
        var emittedClasses = new (string Class, string Css, string Source)[]
        {
            ("aeterni-icon-button--square", iconButtonCss, "IconButton.Shape=Square"),
            ("aeterni-icon-button--tone-muted", iconButtonCss, "IconButton.Tone=Muted"),
            ("aeterni-icon-button--tone-danger-tinted", iconButtonCss, "IconButton.Tone=DangerTinted"),
            ("is-compact", iconButtonCss, "IconButton.Compact"),
            ("aeterni-button--content-start", buttonCss, "Button.ContentAlignment=Start"),
            ("aeterni-icon--tone-muted", iconCss, "Icon.Tone=Muted"),
            ("aeterni-icon--tone-tertiary", iconCss, "Icon.Tone=Tertiary")
        };
        foreach (var (className, css, source) in emittedClasses)
        {
            Require(Regex.IsMatch(css, $@"\.{Regex.Escape(className)}(?![-\w])"),
                $"{source} emits the class '{className}' which must have a CSS consumer.");
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
