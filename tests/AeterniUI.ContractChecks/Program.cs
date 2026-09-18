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

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"Contract check failed: {failure}");
    }

    return 1;
}

Console.WriteLine("Component contract checks passed (7 groups).");
return 0;

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
    Require(html.Contains("is-full-width", StringComparison.Ordinal), "MenuButton FullWidth class must render.");
    Require(html.Contains("aria-disabled=\"true\"", StringComparison.Ordinal), "MenuButton root must expose disabled composite state.");
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
