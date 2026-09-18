using System.Text.RegularExpressions;
using AeterniUI.Components.Accordion;
using AeterniUI.Components.Avatar;
using AeterniUI.Components.DatePicker;
using AeterniUI.Components.Menu;
using AeterniUI.Components.MenuButton;
using AeterniUI.Components.TimePicker;
using AeterniUI.Enums;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IJSRuntime, NoopJsRuntime>();
services.AddAeterniUI();
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

var failures = new List<string>();

await CheckDatePickerRootAsync();
await CheckMenuButtonRootAsync();
await CheckAccordionAriaAsync();
await CheckCalendarGridAsync();
await CheckTimeListFocusAsync();
await CheckAvatarVariantsAsync();
await CheckToolbarAsync();
await CheckToggleGroupAsync();
await CheckSplitButtonAsync();
await CheckPaginationGeometryAsync();
await CheckListAsync();

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"Contract check failed: {failure}");
    }

    return 1;
}

Console.WriteLine("Component contract checks passed (11 groups).");
return 0;

async Task CheckListAsync()
{
    var args = new Dictionary<string, object?> { ["Items"] = new string?[] { "Alpha", null, "<Beta>" } };
    var html = await RenderAsync<AeterniUI.Components.List.List<string?>>(args);
    Require(html.Contains("Alpha") && html.Contains("&lt;Beta&gt;") && !html.Contains("role=\"option\""), "List defaults to encoded text, empty null and display-only rows.");
    args["ItemTemplate"] = (RenderFragment<string?>)(item => builder => builder.AddContent(0, $"row:{item}"));
    html = await RenderAsync<AeterniUI.Components.List.List<string?>>(args);
    Require(html.Contains("row:Alpha"), "ItemTemplate receives the typed item.");
    args["CardMode"] = true;
    html = await RenderAsync<AeterniUI.Components.List.List<string?>>(args);
    Require(Regex.Matches(html, "class=\"aeterni-card ").Count == 3 && !html.Contains("row:"), "Default cards must own their shell and never fall back to ItemTemplate.");
    args["CardTemplate"] = (RenderFragment<string?>)(item => builder => builder.AddContent(0, $"card:{item}"));
    html = await RenderAsync<AeterniUI.Components.List.List<string?>>(args);
    Require(html.Contains("card:Alpha") && !html.Contains("row:"), "CardTemplate supplies card content only.");
    args["SelectionMode"] = SelectionMode.Multiple;
    args["SelectedValues"] = new object?[] { "Alpha" };
    args["Disabled"] = true;
    args["Id"] = "contract-list";
    args["Class"] = "consumer-list";
    args["Style"] = "margin: 0";
    args["AriaLabel"] = "Cards";
    args["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "list" };
    html = await RenderAsync<AeterniUI.Components.List.List<string?>>(args);
    foreach (var expected in new[] { "role=\"listbox\"", "aria-multiselectable=\"true\"", "aria-selected=\"true\"", "aria-disabled=\"true\"", "tabindex=\"-1\"", "id=\"contract-list\"", "consumer-list", "margin: 0", "data-contract=\"list\"", "aria-label=\"Cards\"" })
        Require(html.Contains(expected), $"Card lists must preserve {expected}.");
    Require(Regex.Matches(html, "role=\"option\"").Count == 3, "Each Card list item must have exactly one option, not a nested interactive Card.");
    var model = await RenderAsync<AeterniUI.Components.List.List<(string Name, int Count)>>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { ("Model", 7) },
        ["ItemTemplate"] = (RenderFragment<(string Name, int Count)>)(item => builder => builder.AddContent(0, $"{item.Name}:{item.Count}"))
    });
    Require(model.Contains("Model:7"), "Model templates preserve their item type.");

    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        ListContractHost host = null!;
        var root = await renderer.RenderComponentAsync<ListContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        { ["Ready"] = (Action<ListContractHost>)(value => host = value) }));
        var keyboard = typeof(AeterniUI.Components.List.List<string?>).GetMethod("HandleKeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Task Key(string key) => (Task)keyboard.Invoke(host.List, [new KeyboardEventArgs { Key = key }])!;
        await Key("End");
        Require(Equals(host.Selected, "Gamma") && root.ToHtmlString().Contains("aria-activedescendant"), "Keyboard selects last row and reports active option.");
        host.Rows = ["Replacement", "Alpha"]; host.Update();
        Require(host.Selected is null && !root.ToHtmlString().Contains("aria-activedescendant"), "Removed selection and active descendant must be cleared.");
        await Key("Home");
        Require(Equals(host.Selected, "Replacement"), "Reused row handles must select updated values.");
        host.Disabled = true; host.Update(); await Key("End");
        Require(Equals(host.Selected, "Replacement") && root.ToHtmlString().Contains("tabindex=\"-1\""), "Disabled list ignores keys and leaves Tab sequence.");
        host.Disabled = false; host.Mode = SelectionMode.Multiple; host.SelectedMany = ["Replacement", "Alpha"]; host.Update();
        host.Rows = ["Alpha"]; host.Update();
        Require(host.SelectedMany.SequenceEqual(new object?[] { "Alpha" }), "Dynamic Items prune missing multi-selection values.");
        await Key("Home"); await Key("Enter");
        Require(host.SelectedMany.Count == 0, "Multiple keyboard activation toggles current row.");
        host.Rows = []; host.Update(); await Key("Enter");
        Require(!root.ToHtmlString().Contains("role=\"option\"") && !root.ToHtmlString().Contains("aria-activedescendant"), "Empty Items remove registered rows and active descendant.");
        host.Declarative = true; host.Rows = ["Disabled", "Enabled"]; host.RowDisabled = true; host.Mode = SelectionMode.Single; host.Update();
        await Key("Home");
        Require(Equals(host.Selected, "Enabled") && root.ToHtmlString().Contains("aria-disabled=\"true\""), "Declarative children inherit owner and skip disabled rows.");
        host.Rows = ["Disabled", "Changed"]; host.Update(); await Key("End");
        Require(Equals(host.Selected, "Changed"), "Declarative handle values update when parameters change.");
        host.Rows = []; host.Update();
        Require(!root.ToHtmlString().Contains("aria-activedescendant"), "Disposing the focused declarative row clears its active descendant.");
    });
}

async Task CheckToggleGroupAsync()
{
    var items = new AeterniUI.Components.ToggleGroup.ToggleGroupItem[] { new("a", "Alpha"), new("b", "Beta", true), new("c", "Gamma") };
    var parameters = new Dictionary<string, object?>
    {
        ["Items"] = items, ["AriaLabel"] = "Actions", ["SelectedValues"] = new[] { "b" },
        ["Id"] = "contract-toggle", ["Class"] = "consumer-toggle", ["Style"] = "margin: 0",
        ["Orientation"] = Orientation.Vertical, ["Size"] = Size.Small,
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "toggle" }
    };
    var html = await RenderAsync<AeterniUI.Components.ToggleGroup.ToggleGroup>(parameters);
    foreach (var value in new[] { "role=\"group\"", "aria-label=\"Actions\"", "data-orientation=\"vertical\"", "id=\"contract-toggle\"", "consumer-toggle", "margin: 0", "data-contract=\"toggle\"", "aeterni-toggle-group--sm", "type=\"button\"", "aria-pressed=\"false\"" })
        Require(html.Contains(value), $"ToggleGroup must render {value}.");
    Require(Regex.Matches(html, "aria-pressed=\"true\"").Count == 1 && html.Contains("disabled"), "Disabled selection must remain pressed.");
    Require(!html.Contains("aria-checked") && !html.Contains("aria-orientation"), "Action groups must not pretend to be radio groups or expose unsupported orientation ARIA.");
    foreach (var size in new[] { Size.Small, Size.Default, Size.Large })
    {
        parameters["Size"] = size;
        var sized = await RenderAsync<AeterniUI.Components.ToggleGroup.ToggleGroup>(parameters);
        Require(sized.Contains("aeterni-toggle-group--sm") == (size == Size.Small)
            && sized.Contains("aeterni-toggle-group--lg") == (size == Size.Large),
            $"ToggleGroup {size} must render only its matching size modifier.");
        Require(Regex.Matches(sized, "aria-pressed=\"true\"").Count == 1,
            "Changing ToggleGroup size must preserve selected state.");
    }
    parameters["SelectionMode"] = SelectionMode.Multiple;
    parameters["SelectedValues"] = new[] { "a", "b" };
    parameters["Visible"] = false;
    parameters["Disabled"] = true;
    html = await RenderAsync<AeterniUI.Components.ToggleGroup.ToggleGroup>(parameters);
    Require(Regex.Matches(html, "aria-pressed=\"true\"").Count == 2 && html.Contains("hidden") && html.Contains("inert"), "Multiple state and root visibility/disable contract must render.");
    foreach (var invalid in new Dictionary<string, object?>[]
    {
        new() { ["SelectionMode"] = SelectionMode.None }, new() { ["SelectionMode"] = (SelectionMode)99 },
        new() { ["Orientation"] = (Orientation)99 }, new() { ["Size"] = (Size)99 }, new() { ["AriaLabel"] = " " },
        new() { ["Items"] = null }, new() { ["SelectedValues"] = null },
        new() { ["Items"] = new AeterniUI.Components.ToggleGroup.ToggleGroupItem[] { new(" ", "Empty ID") } },
        new() { ["Items"] = new AeterniUI.Components.ToggleGroup.ToggleGroupItem[] { new("a", "Alpha"), new("a", "Duplicate") } },
        new() { ["Items"] = new AeterniUI.Components.ToggleGroup.ToggleGroupItem[] { new("a", " ") } },
        new() { ["SelectedValues"] = new[] { "unknown" } },
        new() { ["SelectedValues"] = new[] { "a", "a" } },
        new() { ["SelectionMode"] = SelectionMode.Single, ["SelectedValues"] = new[] { "a", "b" } }
    })
    {
        var args = new Dictionary<string, object?> { ["Items"] = items, ["AriaLabel"] = "Actions", ["SelectionMode"] = SelectionMode.Multiple };
        foreach (var pair in invalid) args[pair.Key] = pair.Value;
        try { await RenderAsync<AeterniUI.Components.ToggleGroup.ToggleGroup>(args); Require(false, "ToggleGroup must reject invalid parameters."); }
        catch (ArgumentException) { }
    }

    // Exercise proposals independently of a renderer: a parent may reject or delay them.
    var component = new AeterniUI.Components.ToggleGroup.ToggleGroup();
    IReadOnlyList<string>? proposal = null;
    void Set(string name, object value) => component.GetType().GetProperty(name)!.SetValue(component, value);
    Set("Items", items);
    Set("SelectedValues", new[] { "a" });
    Set("SelectedValuesChanged", EventCallback.Factory.Create<IReadOnlyList<string>>(new object(), (IReadOnlyList<string> value) => proposal = value));
    var toggle = component.GetType().GetMethod("ToggleAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    Task Toggle(string id) => (Task)toggle.Invoke(component, [id])!;
    await Toggle("a");
    Require(proposal is { Count: 0 } && component.SelectedValues.SequenceEqual(["a"]), "Single repeated activation proposes clearing without mutating controlled state.");
    await Toggle("c");
    Require(proposal!.SequenceEqual(["c"]) && component.SelectedValues.SequenceEqual(["a"]), "Single activation proposes replacement only.");
    proposal = null;
    await Toggle("b"); await Toggle("missing");
    Require(proposal is null, "Disabled and stale actions must not emit proposals.");
    Set("SelectionMode", SelectionMode.Multiple);
    await Toggle("c"); Require(proposal!.SequenceEqual(["a", "c"]), "Multiple activation adds a selected action.");
    Set("SelectedValues", proposal!);
    await Toggle("a"); Require(proposal!.SequenceEqual(["c"]), "Multiple activation removes only its action.");
    proposal = null; Set("Disabled", true); await Toggle("c");
    Require(proposal is null, "Disabled groups must not emit proposals.");
    Set("Disabled", false); Set("Visible", false); await Toggle("c");
    Require(proposal is null, "Hidden groups must not emit proposals.");
}

async Task CheckSplitButtonAsync()
{
    var parameters = new Dictionary<string, object?>
    {
        ["Label"] = "Save", ["AriaLabel"] = "Save document", ["MenuAriaLabel"] = "More save actions",
        ["Items"] = new MenuGroup[] { new("actions", "Actions", [new("copy", "Copy")]) },
        ["Id"] = "contract-split", ["Class"] = "consumer-split", ["Style"] = "margin: 0",
        ["Visible"] = false, ["FullWidth"] = true,
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "split", ["dir"] = "rtl" }
    };
    var html = await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
    var root = Regex.Match(html, "^<div[^>]*>").Value;
    foreach (var value in new[] { "id=\"contract-split\"", "consumer-split", "margin: 0", "hidden", "is-full-width", "data-contract=\"split\"", "dir=\"rtl\"" })
        Require(root.Contains(value), $"SplitButton root must render {value}.");
    Require(!root.Contains("disabled"), "SplitButton root must not have native disabled.");
    var buttons = Regex.Matches(html, "<button[^>]*>").Select(m => m.Value).ToArray();
    Require(buttons.Count(button => button.Contains("aeterni-button--split-")) == 2 && html.Contains("hidden"), "SplitButton has two owned triggers; closed popup content remains hidden.");
    Require(buttons.Take(2).All(button => button.Contains("aeterni-button--sm") && button.Contains("aeterni-button--ghost") && button.Contains("aeterni-button--neutral")), "SplitButton defaults to compact small neutral ghost segments.");
    Require(buttons[0].Contains("aria-label=\"Save document\"") && buttons[1].Contains("aria-label=\"More save actions\""), "Split actions need independent accessible names.");
    Require(buttons[0].Contains("aeterni-button--split-primary") && buttons[1].Contains("aeterni-button--split-menu"), "Only owned buttons receive connected geometry.");
    Require(!buttons[0].Contains("aria-haspopup") && buttons[1].Contains("aria-haspopup=\"menu\""), "Only secondary action owns menu semantics.");
    foreach (var state in new[] { "PrimaryDisabled", "MenuDisabled", "Loading", "Disabled" })
    {
        parameters[state] = true;
        html = await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
        buttons = Regex.Matches(html, "<button[^>]*>").Select(m => m.Value).ToArray();
        Require(buttons[0].Contains("disabled") == (state != "MenuDisabled"), $"{state} primary disabled contract.");
        Require(buttons[1].Contains("disabled") == (state is "MenuDisabled" or "Disabled"), $"{state} secondary disabled contract.");
        Require(buttons[0].Contains("aria-busy=\"true\"") == (state == "Loading") && !buttons[1].Contains("aria-busy"), "Loading belongs only to primary action.");
        parameters.Remove(state);
    }
    foreach (var size in Enum.GetValues<Size>())
    foreach (var variant in Enum.GetValues<ButtonVariant>())
    {
        parameters["Size"] = size;
        parameters["Variant"] = variant;
        html = await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
        Require(Regex.Matches(html, "aeterni-button--split-").Count == 2, "All button sizes and variants retain both connected segments.");
    }
    parameters["Open"] = true;
    parameters["OpenChanged"] = EventCallback.Factory.Create<bool>(new object(), (bool _) => { });
    html = await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
    Require(html.Contains("aria-expanded=\"true\"") && html.Contains("id=\"contract-split-actions-menu\""), "Controlled menu references the rendered menu id.");
    RenderFragment nestedButton = builder =>
    {
        builder.OpenComponent<AeterniUI.Components.Button.Button>(0);
        builder.AddAttribute(1, "AriaLabel", "Nested button");
        builder.CloseComponent();
    };
    parameters["Items"] = new MenuGroup[] { new("actions", "Actions", [new("copy", "Copy", Icon: nestedButton)]) };
    html = await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
    Require(Regex.Matches(html, "aeterni-button--split-").Count == 2, "Split context must not style nested menu buttons.");
    parameters["MenuDisabled"] = true;
    html = await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
    Require(html.Contains("aria-expanded=\"false\""), "Menu disabled overrides controlled open.");
    parameters["MenuAriaLabel"] = " ";
    try
    {
        await RenderAsync<AeterniUI.Components.SplitButton.SplitButton>(parameters);
        Require(false, "SplitButton must reject missing menu name.");
    }
    catch (ArgumentException) { }
}

async Task CheckToolbarAsync()
{
    var html = await RenderAsync<AeterniUI.Components.Toolbar.Toolbar>(new Dictionary<string, object?>
    {
        ["AriaLabel"] = "Document actions", ["Id"] = "contract-toolbar", ["Class"] = "consumer-toolbar",
        ["Style"] = "margin: 0", ["Visible"] = false, ["Disabled"] = true,
        ["Orientation"] = Orientation.Vertical,
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "toolbar" }
    });
    foreach (var value in new[] { "role=\"toolbar\"", "aria-label=\"Document actions\"", "aria-orientation=\"vertical\"", "aria-disabled=\"true\"", "id=\"contract-toolbar\"", "consumer-toolbar", "margin: 0", "data-contract=\"toolbar\"", "hidden", "inert", "tabindex=\"-1\"" })
        Require(html.Contains(value, StringComparison.Ordinal), $"Toolbar must render {value}.");
    var group = await RenderAsync<AeterniUI.Components.Toolbar.ToolbarGroup>(new Dictionary<string, object?> { ["AriaLabel"] = "Editing", ["Disabled"] = true });
    Require(group.Contains("role=\"group\"") && group.Contains("aria-label=\"Editing\"") && group.Contains("inert") && !group.Contains("tabindex"), "Named toolbar groups must not introduce tab stops.");
    try
    {
        await RenderAsync<AeterniUI.Components.Toolbar.Toolbar>(new Dictionary<string, object?> { ["AriaLabel"] = "Actions", ["Orientation"] = (Orientation)99 });
        Require(false, "Toolbar must reject invalid orientation.");
    }
    catch (ArgumentOutOfRangeException) { }
    try
    {
        await RenderAsync<AeterniUI.Components.Toolbar.Toolbar>(new Dictionary<string, object?> { ["AriaLabel"] = " " });
        Require(false, "Toolbar must require a name.");
    }
    catch (ArgumentException) { }
}

async Task CheckDatePickerRootAsync()
{
    await CheckFieldSizesAsync<DatePicker>("DatePicker", "date-picker");
    await CheckFieldSizesAsync<DateRangePicker>("DatePicker", "date-picker");
    await CheckFieldSizesAsync<TimePicker>("TimePicker", "time-picker");
    await CheckFieldSizesAsync<AeterniUI.Components.DateTimePicker.DateTimePicker>("DateTimePicker", "date-time-picker");

    async Task CheckFieldSizesAsync<TComponent>(string folder, string className) where TComponent : IComponent
    {
        var css = await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), "src", "AeterniUI", "Components", folder, $"{typeof(TComponent).Name}.razor.css"));
        var trigger = $".aeterni-{className}__trigger";
        var baseRule = Regex.Match(css, Regex.Escape(trigger) + @"\s*\{([^}]+)\}").Groups[1].Value;
        Require(baseRule.Contains("padding: 0 var(--aeterni-control-padding-x-md);", StringComparison.Ordinal), $"{typeof(TComponent).Name} default padding must use the medium control token.");
        Require(baseRule.Contains("font-size: var(--aeterni-font-size-sm);", StringComparison.Ordinal), $"{typeof(TComponent).Name} default text must match Input.");
        Require(baseRule.Contains("line-height: var(--aeterni-line-height-normal);", StringComparison.Ordinal), $"{typeof(TComponent).Name} must retain normal field line-height.");

        foreach (var (size, suffix, font) in new[] { (Size.Small, "sm", "xs"), (Size.Default, "md", "sm"), (Size.Large, "lg", "base") })
        {
            var sizedHtml = await RenderAsync<TComponent>(new Dictionary<string, object?> { ["Size"] = size });
            var modifier = $"aeterni-{className}--{suffix}";
            Require(sizedHtml.Contains($"aeterni-{className}__trigger", StringComparison.Ordinal), $"{typeof(TComponent).Name} must render its styled trigger.");
            if (size == Size.Default)
                continue;

            Require(sizedHtml.Contains(modifier, StringComparison.Ordinal), $"{typeof(TComponent).Name} must render its {size} modifier.");
            var rule = Regex.Match(css, Regex.Escape($"{trigger}.{modifier}") + @"\s*\{([^}]+)\}").Groups[1].Value;
            Require(rule.Contains($"padding-inline: var(--aeterni-control-padding-x-{suffix});", StringComparison.Ordinal), $"{typeof(TComponent).Name} {size} must use its control padding token.");
            Require(rule.Contains($"font-size: var(--aeterni-font-size-{font});", StringComparison.Ordinal), $"{typeof(TComponent).Name} {size} text must match Input.");
            Require(rule.Contains($"min-height: var(--aeterni-height-{suffix});", StringComparison.Ordinal), $"{typeof(TComponent).Name} {size} must preserve its height.");
        }
    }

    var html = await RenderAsync<DatePicker>(new Dictionary<string, object?>
    {
        [nameof(DatePicker.Id)] = "contract-date",
        [nameof(DatePicker.Class)] = "consumer-date",
        [nameof(DatePicker.Style)] = "inline-size: 12rem",
        [nameof(DatePicker.Visible)] = false,
        [nameof(DatePicker.AdditionalAttributes)] = new Dictionary<string, object> { ["data-contract"] = "date" }
    });

    Require(!html.Contains("is-full-width", StringComparison.Ordinal), "DatePicker must remain compact by default.");
    var fullWidthHtml = await RenderAsync<DatePicker>(new Dictionary<string, object?>
    {
        [nameof(DatePicker.FullWidth)] = true
    });
    Require(fullWidthHtml.Contains("is-full-width", StringComparison.Ordinal), "DatePicker FullWidth must expose its root layout class.");

    foreach (var fullWidth in new[] { false, true })
    {
        var dateTimeHtml = await RenderAsync<AeterniUI.Components.DateTimePicker.DateTimePicker>(new Dictionary<string, object?>
        {
            [nameof(DatePicker.FullWidth)] = fullWidth
        });
        var timeHtml = await RenderAsync<TimePicker>(new Dictionary<string, object?>
        {
            [nameof(TimePicker.FullWidth)] = fullWidth
        });
        Require(dateTimeHtml.Contains("is-full-width", StringComparison.Ordinal) == fullWidth, "DateTimePicker must reflect FullWidth on its root.");
        Require(timeHtml.Contains("is-full-width", StringComparison.Ordinal) == fullWidth, "TimePicker must reflect FullWidth on its root.");
    }

    Require(html.Contains("id=\"contract-date\"", StringComparison.Ordinal), "DatePicker root must forward Id.");
    Require(html.Contains("consumer-date", StringComparison.Ordinal) && html.Contains("aeterni-date-picker", StringComparison.Ordinal), "DatePicker root must merge consumer and component classes.");
    Require(html.Contains("inline-size: 12rem", StringComparison.Ordinal), "DatePicker root must forward Style.");
    Require(html.Contains("data-contract=\"date\"", StringComparison.Ordinal), "DatePicker root must forward unmatched attributes.");
    Require(Regex.IsMatch(html, "<div[^>]*\\shidden(?:=|\\s|>)", RegexOptions.CultureInvariant), "DatePicker root must reflect Visible=false.");
}

async Task CheckMenuButtonRootAsync()
{
    var menuCss = await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), "src", "AeterniUI", "Components", "Menu", "Menu.razor.css"));
    Require(menuCss.Contains("outline-offset: calc(-1 * var(--aeterni-focus-width))"), "Menu focus outline must stay inside rows to avoid collapse and scroll clipping.");
    var html = await RenderAsync<MenuButton>(new Dictionary<string, object?>
    {
        [nameof(MenuButton.Id)] = "contract-menu-button",
        [nameof(MenuButton.Class)] = "consumer-menu-button",
        [nameof(MenuButton.Label)] = "Actions",
        [nameof(MenuButton.Items)] = Array.Empty<MenuGroup>(),
        [nameof(MenuButton.FullWidth)] = true,
        [nameof(MenuButton.Disabled)] = true
    });

    Require(html.Contains("id=\"contract-menu-button\"", StringComparison.Ordinal), "MenuButton root must forward Id.");
    Require(html.Contains("aeterni-menu-button", StringComparison.Ordinal) && html.Contains("consumer-menu-button", StringComparison.Ordinal), "MenuButton root must merge classes.");
    Require(html.Contains("--aeterni-popover-content-padding: var(--aeterni-spacing-1)", StringComparison.Ordinal), "MenuButton must scope compact surface padding to its own popover.");
    Require(html.Contains("--aeterni-menu-item-padding: var(--aeterni-control-padding-x-sm)", StringComparison.Ordinal), "MenuButton must scope compact row padding to its own menu.");
    Require(!html.Contains("aeterni-list"), "MenuButton renders Menu, not List.");
    Require(!html.Contains("--aeterni-menu-group-indent"), "Popup menus retain Menu's existing child indentation.");
    var groupedMenu = await RenderAsync<AeterniUI.Components.Menu.Menu>(new Dictionary<string, object?>
    {
        ["Items"] = new MenuGroup[]
        {
            new("open", "Open", [new("first", "First")]),
            new("closed", "Closed", [new("second", "Second")], InitiallyOpen: false),
            new("empty", "Empty", [new("hidden", "Hidden", Visible: false)])
        }
    });
    var groups = Regex.Matches(groupedMenu, "<li class=\"aeterni-menu__group ([^\"]*)\"").Select(match => match.Groups[1].Value).ToArray();
    Require(groups.Length == 3 && groups[0].Contains("is-open") && groups[0].Contains("has-items")
        && !groups[1].Contains("is-open") && groups[1].Contains("has-items")
        && groups[2].Contains("is-open") && !groups[2].Contains("has-items"),
        "Only expanded groups with visible children may receive a header-to-items gap.");
    Require(groupedMenu.Contains("aria-expanded=\"false\""), "Collapsed Menu headers retain disclosure semantics.");
    var plainPopover = await RenderAsync<AeterniUI.Components.Popup.Popover>(new Dictionary<string, object?>());
    Require(!plainPopover.Contains("--aeterni-popover-content-padding"), "Unrelated content popovers retain their default padding.");
    Require(html.Contains("is-full-width", StringComparison.Ordinal), "MenuButton FullWidth class must render.");
    Require(html.Contains("aria-disabled=\"true\"", StringComparison.Ordinal), "MenuButton root must expose disabled composite state.");
    var controlledId = Regex.Match(html, "aria-controls=\"([^\"]+)\"").Groups[1].Value;
    Require(!string.IsNullOrEmpty(controlledId) && Regex.IsMatch(html, $"<nav[^>]*id=\"{Regex.Escape(controlledId)}\""), "MenuButton aria-controls must resolve to its real menu root.");
}

async Task CheckAccordionAriaAsync()
{
    RenderFragment content = builder =>
    {
        builder.OpenElement(0, "button");
        builder.AddContent(1, "Focusable content");
        builder.CloseElement();
    };
    var html = await RenderAsync<Accordion>(new Dictionary<string, object?>
    {
        [nameof(Accordion.Items)] = new[] { new AccordionItem("closed", "Closed", content) }
    });

    Require(html.Contains("aria-hidden=\"true\"", StringComparison.Ordinal), "Accordion closed panel must emit an explicit ARIA string value.");
    Require(Regex.IsMatch(html, "<div[^>]*\\sinert(?:=|\\s|>)", RegexOptions.CultureInvariant), "Accordion closed panel must be inert.");
}

async Task CheckTimeListFocusAsync()
{
    var html = await RenderAsync<TimeOptionList>(new Dictionary<string, object?>
    {
        [nameof(TimeOptionList.Value)] = new TimeOnly(9, 30),
        [nameof(TimeOptionList.Step)] = TimeSpan.FromMinutes(15),
        [nameof(TimeOptionList.MinTime)] = new TimeOnly(8, 0),
        [nameof(TimeOptionList.MaxTime)] = new TimeOnly(10, 0)
    });

    Require(Regex.Matches(html, "tabindex=\"0\"", RegexOptions.CultureInvariant).Count == 1, "TimeOptionList must expose exactly one Tab stop.");
    Require(html.Contains("aria-activedescendant=", StringComparison.Ordinal), "TimeOptionList must identify its active option.");
    Require(!Regex.IsMatch(html, "role=\"option\"[^>]*tabindex", RegexOptions.CultureInvariant), "Time options must not be individual Tab stops.");
    Require(Regex.Matches(html, "role=\"listbox\"", RegexOptions.CultureInvariant).Count == 3, "TimeOptionList must always expose hour, minute, and second wheels.");

    var secondsHtml = await RenderAsync<TimeOptionList>(new Dictionary<string, object?>
    {
        [nameof(TimeOptionList.Value)] = new TimeOnly(9, 30, 45),
        [nameof(TimeOptionList.Step)] = TimeSpan.FromSeconds(1),
        [nameof(TimeOptionList.TimeFormat)] = TimeFormat.TwentyFourHour
    });
    var renderedOptions = Regex.Matches(secondsHtml, "role=\"option\"", RegexOptions.CultureInvariant).Count;
    Require(secondsHtml.Contains("Second", StringComparison.Ordinal), "A second-granularity time picker must render the seconds column.");
    Require(secondsHtml.Contains(">Cancel<", StringComparison.Ordinal) && secondsHtml.Contains(">Confirm<", StringComparison.Ordinal), "TimeOptionList must render explicit cancel and confirm actions.");
    Require(renderedOptions <= 144, "Second granularity must not render all 86,400 daily values into the DOM.");

    var pickerHtml = await RenderAsync<TimePicker>(new Dictionary<string, object?>
    {
        [nameof(TimePicker.Value)] = new TimeOnly(9, 5, 7),
        [nameof(TimePicker.FullWidth)] = true
    });
    Require(pickerHtml.Contains("09:05:07", StringComparison.Ordinal), "TimePicker must default to 24-hour HH:mm:ss display.");
    Require(pickerHtml.Contains("is-full-width", StringComparison.Ordinal), "TimePicker FullWidth must emit its public modifier class.");

    Require(Regex.Matches(secondsHtml, "aeterni-time-option-list__label").Count == renderedOptions,
        "Every option must isolate its cylinder visual from its semantic hit area.");
    var probe = new TimeOptionList();
    const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var type = typeof(TimeOptionList);
    void Set(string name, object value) => type.GetProperty(name)!.SetValue(probe, value);
    void Parameters() => type.GetMethod("OnParametersSet", flags)!.Invoke(probe, null);
    TimeOnly? Draft() => (TimeOnly?)type.GetField("_draftValue", flags)!.GetValue(probe);
    Set("Value", new TimeOnly(9, 30, 15));
    Parameters();
    type.GetField("_draftValue", flags)!.SetValue(probe, new TimeOnly(10, 42, 35));
    Parameters();
    Require(Draft() == new TimeOnly(10, 42, 35), "Unchanged host parameters must preserve a local wheel draft.");
    Set("Value", new TimeOnly(18, 12, 25));
    Parameters();
    Require(Draft() == new TimeOnly(18, 12, 25), "External Value must replace the draft.");
    Require((bool)type.GetField("_animateActiveIntoView", flags)!.GetValue(probe)!, "External Value must request animated alignment.");
    Set("TimeFormat", TimeFormat.TwelveHour);
    Parameters();
    Require(Draft() == new TimeOnly(18, 12, 25), "Format changes must not change the selected time.");
    Set("Step", TimeSpan.FromSeconds(10));
    Set("MinTime", new TimeOnly(18, 12, 30));
    Set("MaxTime", new TimeOnly(18, 12, 50));
    Set("DisabledTime", (Func<TimeOnly, bool>)(time => time.Second == 30));
    Parameters();
    Require(Draft() == new TimeOnly(18, 12, 40), "Step/bounds/disabled changes must resolve the nearest available time in C#.");

    var root = FindRepositoryRoot();
    var css = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/TimePicker/TimePicker.razor.css"));
    Require(css.Contains(".aeterni-time-picker.is-full-width", StringComparison.Ordinal), "TimePicker FullWidth class must have a matching style rule.");
}

async Task CheckCalendarGridAsync()
{
    var html = await RenderAsync<DateCalendar>(new Dictionary<string, object?>
    {
        [nameof(DateCalendar.DisplayMonth)] = new DateOnly(2026, 9, 1),
        [nameof(DateCalendar.VisibleMonths)] = 1
    });

    Require(Regex.Matches(html, "role=\"row\"", RegexOptions.CultureInvariant).Count == 6, "DateCalendar must render six explicit grid rows.");
    Require(Regex.Matches(html, "tabindex=\"0\"", RegexOptions.CultureInvariant).Count == 1, "DateCalendar must expose exactly one roving Tab stop.");
    Require(Regex.Matches(html, "role=\"gridcell\"", RegexOptions.CultureInvariant).Count == 42, "DateCalendar must render gridcell semantics for every day cell.");
}

async Task CheckPaginationGeometryAsync()
{
    var css = await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), "src", "AeterniUI", "Components", "Pagination", "Pagination.razor.css"));
    foreach (var rule in new[] { "place-items: center", "box-sizing: border-box", "line-height: var(--aeterni-leading-none)", "font-size: var(--aeterni-font-size-xs)", "font-variant-numeric: tabular-nums", "text-align: center" })
        Require(css.Contains(rule), $"Pagination must retain centered numeric geometry: {rule}.");
    Require(!css.Contains("transform: scale"), "Current page must not scale text onto fractional pixels.");
}

async Task CheckAvatarVariantsAsync()
{
    var html = await RenderAsync<Avatar>(new Dictionary<string, object?>
    {
        [nameof(Avatar.Name)] = "Ada Lovelace",
        [nameof(Avatar.Size)] = Size.Default,
        [nameof(Avatar.Color)] = Color.Info
    });

    Require(html.Contains("aeterni-avatar--info", StringComparison.Ordinal), "Avatar Info must emit its public variant class.");
    Require(!html.Contains("aeterni-avatar--default", StringComparison.Ordinal), "Avatar default size must not emit a modifier class.");

    var root = FindRepositoryRoot();
    var css = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/Avatar/Avatar.razor.css"));
    Require(css.Contains(".aeterni-avatar--info", StringComparison.Ordinal), "Avatar Info class must have a matching style rule.");
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

void Require(bool condition, string message)
{
    if (!condition)
    {
        failures.Add(message);
    }
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aeterni_ui.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
}

internal sealed class NoopJsRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        ValueTask.FromResult(default(TValue)!);
}
