using System.Globalization;
using System.Text.RegularExpressions;
using AeterniUI.Components.Accordion;
using AeterniUI.Components.Autocomplete;
using AeterniUI.Components.Avatar;
using AeterniUI.Components.Breadcrumb;
using AeterniUI.Components.DatePicker;
using AeterniUI.Components.DateTimePicker;
using AeterniUI.Components.Descriptions;
using AeterniUI.Components.Dialog;
using AeterniUI.Components.FlashCard;
using AeterniUI.Components.FlashCardGroup;
using AeterniUI.Components.InputNumber;
using AeterniUI.Components.Menu;
using AeterniUI.Components.MenuButton;
using AeterniUI.Components.MultiSelect;
using AeterniUI.Components.Search;
using AeterniUI.Components.Segmented;
using AeterniUI.Components.Slider;
using AeterniUI.Components.Stepper;
using AeterniUI.Components.Timeline;
using AeterniUI.Components.TimePicker;
using AeterniUI.Components.VirtualList;
using AeterniUI.Enums;
using AeterniUI.Icons;
using AeterniUI.Models.Dialog;
using AeterniUI.Services;
using AeterniUI.Services.Impl;
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
await CheckFlashCardAsync();
await CheckFlashCardGroupAsync();
await CheckToolbarAsync();
await CheckToggleGroupAsync();
await CheckSplitButtonAsync();
await CheckSearchAsync();
await CheckAutocompleteAsync();
await CheckMultiSelectAsync();
await CheckInputNumberAsync();
await CheckSliderAsync();
await CheckPaginationGeometryAsync();
await CheckBreadcrumbAsync();
await CheckStepperAsync();
await CheckTimelineAsync();
await CheckDescriptionsAsync();
await CheckListAsync();
await CheckVirtualListAsync();
await CheckQualityStatesAsync();
await CheckDateBoundariesAsync();
await CheckOverlayTransitionsAsync();
await CheckArchitectureP0Async();
await CheckReviewBatch23Async();
await CheckReviewBatch24Async();
await CheckReviewBatch25Async();
await CheckReviewBatch26Async();
await CheckJsLifecycleAsync();
await CheckSharedAbstractionsAsync();
await CheckEnumValidationAsync();
await CheckTextTableFallbackAsync();
await CheckTokenSurfaceAsync();
CheckTimeMapReuse();

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"Contract check failed: {failure}");
    }

    return 1;
}

Console.WriteLine("Component contract checks passed (33 groups).");
return 0;

// Priority-7 batch: InputNumber composes Input and Button, so the checks pin the
// composite root contract, the spinbutton semantics added to the inner input and
// the value pipeline (typing, stepping, clamping) driven on a live instance.
async Task CheckInputNumberAsync()
{
    var module = (AeterniUI.Attributes.JsModuleAttribute)Attribute.GetCustomAttribute(
        typeof(InputNumber<decimal>), typeof(AeterniUI.Attributes.JsModuleAttribute))!;
    Require(module.Interactive && module.Name == "inputnumber",
        "InputNumber declares its interactive key-guard module.");

    var html = await RenderAsync<InputNumber<decimal>>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-input-number",
        ["Class"] = "consumer-input-number",
        ["Style"] = "color: inherit",
        ["Value"] = 12.5m,
        ["Min"] = 0m,
        ["Max"] = 100m,
        ["Step"] = 0.5m,
        ["ShowControls"] = true,
        ["Required"] = true,
        ["Invalid"] = true,
        ["Disabled"] = true,
        ["FullWidth"] = true,
        ["Size"] = Size.Small,
        ["Placeholder"] = "Amount",
        ["Name"] = "amount",
        ["AriaLabel"] = "Amount",
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-kind"] = "quantity" }
    });

    var root = Regex.Match(html, "<div[^>]*id=\"contract-input-number\"[^>]*>").Value;
    Require(root.Contains("consumer-input-number", StringComparison.Ordinal)
        && root.Contains("aeterni-input-number--sm", StringComparison.Ordinal)
        && root.Contains("is-full-width", StringComparison.Ordinal)
        && root.Contains("is-invalid", StringComparison.Ordinal)
        && root.Contains("is-disabled", StringComparison.Ordinal)
        && root.Contains("aria-disabled=\"true\"", StringComparison.Ordinal)
        && root.Contains("data-kind=\"quantity\"", StringComparison.Ordinal)
        && root.Contains("style=\"color: inherit", StringComparison.Ordinal),
        "InputNumber preserves root attributes and emits size and state classes.");

    var input = Regex.Match(html, "<input[^>]*role=\"spinbutton\"[^>]*>").Value;
    Require(input.Contains("type=\"text\"", StringComparison.Ordinal)
        && input.Contains("inputmode=\"decimal\"", StringComparison.Ordinal)
        && input.Contains("aria-valuenow=\"12.5\"", StringComparison.Ordinal)
        && input.Contains("aria-valuemin=\"0.0\"", StringComparison.Ordinal)
        && input.Contains("aria-valuemax=\"100.0\"", StringComparison.Ordinal)
        && input.Contains("placeholder=\"Amount\"", StringComparison.Ordinal)
        && input.Contains("name=\"amount\"", StringComparison.Ordinal),
        "InputNumber renders a text input with string spinbutton bounds; the native number type would duplicate the steppers.");
    Require(input.Contains("aria-required=\"true\"", StringComparison.Ordinal)
        && input.Contains("aria-invalid=\"true\"", StringComparison.Ordinal)
        && input.Contains("disabled", StringComparison.Ordinal),
        "InputNumber forwards the field state to the inner input.");

    var buttons = Regex.Matches(html, "<button[^>]*aria-label=\"(Increase|Decrease) value\"[^>]*>");
    Require(html.Contains("role=\"group\"", StringComparison.Ordinal)
        && buttons.Count == 2
        && buttons.All(button => button.Value.Contains("disabled", StringComparison.Ordinal)),
        "ShowControls renders two grouped step buttons that follow the disabled state.");

    var bare = await RenderAsync<InputNumber<int>>(new Dictionary<string, object?>
    {
        ["Value"] = 3
    });
    Require(!bare.Contains("aeterni-icon-button", StringComparison.Ordinal)
        && !bare.Contains("Increase value", StringComparison.Ordinal),
        "InputNumber renders no step buttons unless ShowControls is on.");
    Require(bare.Contains("inputmode=\"numeric\"", StringComparison.Ordinal),
        "Integer value types get the numeric keyboard hint.");

    foreach (var size in Enum.GetValues<Size>())
    {
        var sized = await RenderAsync<InputNumber<int>>(new Dictionary<string, object?>
        {
            ["Value"] = 1,
            ["Size"] = size
        });
        Require(sized.Contains("aeterni-input-number--sm", StringComparison.Ordinal) == (size == Size.Small)
            && sized.Contains("aeterni-input-number--lg", StringComparison.Ordinal) == (size == Size.Large),
            $"InputNumber renders exactly the {size} size modifier.");
    }

    Require(!Regex.IsMatch(html, ">[^<]*[\\u4e00-\\u9fff][^<]*<"),
        "InputNumber must not render CJK text from built-in defaults.");

    var invalidConfigs = new (string Name, Func<Task> Render)[]
    {
        ("InputNumber.Step zero", () => RenderAsync<InputNumber<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Step"] = 0 })),
        ("InputNumber.Step negative", () => RenderAsync<InputNumber<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Step"] = -1 })),
        ("InputNumber.Min above Max", () => RenderAsync<InputNumber<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Min"] = 5, ["Max"] = 1 })),
        ("InputNumber.Precision range", () => RenderAsync<InputNumber<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Precision"] = 16 })),
        ("InputNumber.Precision below step", () => RenderAsync<InputNumber<decimal>>(new Dictionary<string, object?> { ["Value"] = 1m, ["Step"] = 0.5m, ["Precision"] = 0 })),
        ("InputNumber.Min non-finite", () => RenderAsync<InputNumber<double>>(new Dictionary<string, object?> { ["Value"] = 1d, ["Min"] = double.NaN }))
    };

    foreach (var (name, render) in invalidConfigs)
    {
        try
        {
            await render();
            Require(false, $"{name} must reject its invalid configuration instead of degrading silently.");
        }
        catch (ArgumentException) { }
    }

    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        InputNumberContractHost host = null!;
        var root = await renderer.RenderComponentAsync<InputNumberContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<InputNumberContractHost>)(x => host = x)
        }));

        var fieldHtml = root.ToHtmlString();
        var inputTag = Regex.Match(fieldHtml, "<input[^>]*role=\"spinbutton\"[^>]*>").Value;
        var inputId = Regex.Match(inputTag, "id=\"([^\"]+)\"").Groups[1].Value;
        Require(inputId.EndsWith("-input", StringComparison.Ordinal),
            "The inner input adopts the FormField input id.");
        Require(fieldHtml.Contains($"for=\"{inputId}\"", StringComparison.Ordinal),
            "The field label targets the editable element, not the wrapper.");
        Require(inputTag.Contains("aria-describedby=\"", StringComparison.Ordinal),
            "The field description is linked from the input.");

        var press = typeof(InputNumber<decimal>)
            .GetMethod("HandleKeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        async Task Press(string key) =>
            await (Task)press.Invoke(host.Field, [new KeyboardEventArgs { Key = key }])!;

        await Press("ArrowUp");
        Require(host.Changes.SequenceEqual([13m]) && host.Value == 13m,
            $"ArrowUp steps once by Step, got [{string.Join(", ", host.Changes)}].");
        host.Changes.Clear();

        await Press("PageDown");
        Require(host.Changes.SequenceEqual([8m]), "PageDown jumps ten increments at once.");
        host.Changes.Clear();

        host.Value = 99.5m;
        host.Update();
        await Press("ArrowUp");
        Require(host.Changes.SequenceEqual([100m]), "Stepping clamps at Max.");
        host.Changes.Clear();
        await Press("ArrowUp");
        Require(host.Changes.Count == 0, "Stepping at the Max bound reports no change.");

        var typeText = typeof(InputNumber<decimal>)
            .GetMethod("HandleTextChangedAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var blur = typeof(InputNumber<decimal>)
            .GetMethod("HandleBlurAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        host.Changes.Clear();
        await (Task)typeText.Invoke(host.Field, ["abc"])!;
        Require(host.Changes.Count == 0, "Unparseable drafts are never committed.");
        await (Task)typeText.Invoke(host.Field, ["12.75"])!;
        Require(host.Changes.SequenceEqual([12.75m]), "Parseable drafts commit while typing, without rounding.");
        host.Changes.Clear();
        await (Task)blur.Invoke(host.Field, [new FocusEventArgs()])!;
        Require(host.Changes.SequenceEqual([12.8m]), "Blur rounds the draft to the step's precision.");
        host.Changes.Clear();
        await (Task)typeText.Invoke(host.Field, [""])!;
        Require(host.Changes.SequenceEqual([(decimal?)null]), "Clearing the draft commits the empty value.");

        host.Disabled = true;
        host.Update();
        host.Changes.Clear();
        await Press("ArrowUp");
        Require(host.Changes.Count == 0, "Disabled fields ignore step keys.");

        host.Disabled = false;
        host.Invalid = true;
        host.Required = true;
        host.Update();
        var invalidHtml = root.ToHtmlString();
        Require(invalidHtml.Contains("is-invalid", StringComparison.Ordinal)
            && invalidHtml.Contains("aria-invalid=\"true\"", StringComparison.Ordinal)
            && invalidHtml.Contains("aria-required=\"true\"", StringComparison.Ordinal),
            "InputNumber inherits Invalid and Required from its FormField.");
    });

    // The JS key guard exists to stop the page acting on the keys the C# handler
    // steps on, so the two literals have to move together; a change on one side
    // alone would either scroll the page or silently drop the suppression.
    var repositoryRoot = FindRepositoryRoot();
    var code = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "src/AeterniUI/Components/InputNumber/InputNumber.razor.cs"));
    var script = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "src/AeterniUI/Components/InputNumber/InputNumber.razor.js"));
    var codeKeys = KeyLiterals(code);
    Require(codeKeys.SequenceEqual(KeyLiterals(script)),
        $"The InputNumber key guard must match the C# step keys, C# has [{string.Join(", ", codeKeys)}].");
}

// Priority-7 batch: Slider is a container-style field control, so the checks pin
// the slider role on the single focusable root, continuous/stepped input, the
// clamping contract and the pointer/keyboard paths driven on a live instance.
async Task CheckSliderAsync()
{
    var module = (AeterniUI.Attributes.JsModuleAttribute)Attribute.GetCustomAttribute(
        typeof(Slider<int>), typeof(AeterniUI.Attributes.JsModuleAttribute))!;
    Require(module.Interactive && module.Name == "slider",
        "Slider declares its interactive drag module.");

    var html = await RenderAsync<Slider<int>>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-slider",
        ["Class"] = "consumer-slider",
        ["Value"] = 25,
        ["Min"] = 0,
        ["Max"] = 100,
        ["Step"] = 5,
        ["Size"] = Size.Large,
        ["AriaLabel"] = "Volume",
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-kind"] = "volume" }
    });

    var root = Regex.Match(html, "<div[^>]*id=\"contract-slider\"[^>]*>").Value;
    Require(root.Contains("role=\"slider\"", StringComparison.Ordinal)
        && root.Contains("tabindex=\"0\"", StringComparison.Ordinal)
        && root.Contains("aeterni-slider--lg", StringComparison.Ordinal)
        && root.Contains("aria-valuenow=\"25\"", StringComparison.Ordinal)
        && root.Contains("aria-valuemin=\"0\"", StringComparison.Ordinal)
        && root.Contains("aria-valuemax=\"100\"", StringComparison.Ordinal)
        && root.Contains("aria-valuetext=\"25\"", StringComparison.Ordinal)
        && root.Contains("aria-label=\"Volume\"", StringComparison.Ordinal)
        && root.Contains("consumer-slider", StringComparison.Ordinal)
        && root.Contains("data-kind=\"volume\"", StringComparison.Ordinal)
        && root.Contains("--aeterni-slider-value: 0.25", StringComparison.Ordinal),
        "Slider renders the slider role, its value semantics and the position fraction.");
    Require(!root.Contains("aria-orientation", StringComparison.Ordinal),
        "A horizontal slider omits the default orientation.");
    Require(html.Contains("aeterni-slider__fill", StringComparison.Ordinal)
        && html.Contains("aeterni-slider__thumb", StringComparison.Ordinal),
        "Slider renders its decorative fill and thumb.");
    Require(!html.Contains("aeterni-slider__tick", StringComparison.Ordinal)
        && typeof(Slider<int>).GetProperty("ShowTicks") is null,
        "Slider has no tick rendering or ShowTicks API.");

    var disabled = await RenderAsync<Slider<int>>(new Dictionary<string, object?>
    {
        ["Value"] = 10,
        ["Disabled"] = true
    });
    var disabledRoot = Regex.Match(disabled, "<div[^>]*role=\"slider\"[^>]*>").Value;
    Require(disabledRoot.Contains("aria-disabled=\"true\"", StringComparison.Ordinal)
        && disabledRoot.Contains("is-disabled", StringComparison.Ordinal)
        && !disabledRoot.Contains("tabindex", StringComparison.Ordinal),
        "A disabled slider leaves the Tab sequence and announces the state.");

    var clamped = await RenderAsync<Slider<int>>(new Dictionary<string, object?>
    {
        ["Value"] = 999,
        ["Min"] = 0,
        ["Max"] = 100
    });
    Require(clamped.Contains("aria-valuenow=\"100\"", StringComparison.Ordinal)
        && clamped.Contains("--aeterni-slider-value: 1", StringComparison.Ordinal),
        "Out-of-range values clamp instead of throwing.");

    var decimalSlider = await RenderAsync<Slider<decimal>>(new Dictionary<string, object?>
    {
        ["Value"] = 2.5m,
        ["Min"] = 0m,
        ["Max"] = 10m,
        ["Step"] = 0.5m
    });
    Require(decimalSlider.Contains("aria-valuenow=\"2.5\"", StringComparison.Ordinal)
        && decimalSlider.Contains("--aeterni-slider-value: 0.25", StringComparison.Ordinal),
        "Decimal sliders keep their own grid.");

    var continuousSlider = await RenderAsync<Slider<double>>(new Dictionary<string, object?>
    {
        ["Value"] = 0.125d,
        ["Min"] = 0d,
        ["Max"] = 1d,
        ["Precision"] = 3
    });
    Require(continuousSlider.Contains("aria-valuenow=\"0.125\"", StringComparison.Ordinal)
        && continuousSlider.Contains("--aeterni-slider-value: 0.125", StringComparison.Ordinal),
        "A null Step renders a continuous fractional slider without inventing a grid.");

    var vertical = await RenderAsync<Slider<int>>(new Dictionary<string, object?>
    {
        ["Value"] = 25,
        ["Orientation"] = Orientation.Vertical
    });
    Require(vertical.Contains("aeterni-slider--vertical", StringComparison.Ordinal)
        && vertical.Contains("aria-orientation=\"vertical\"", StringComparison.Ordinal),
        "The vertical layout announces its orientation.");

    Require(!Regex.IsMatch(html, ">[^<]*[\\u4e00-\\u9fff][^<]*<"),
        "Slider must not render CJK text from built-in defaults.");

    var invalidConfigs = new (string Name, Func<Task> Render)[]
    {
        ("Slider.Min equal to Max", () => RenderAsync<Slider<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Min"] = 0, ["Max"] = 0 })),
        ("Slider.Step zero", () => RenderAsync<Slider<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Step"] = 0 })),
        ("Slider.Precision range", () => RenderAsync<Slider<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Precision"] = -1 })),
        ("Slider.Max non-finite", () => RenderAsync<Slider<double>>(new Dictionary<string, object?> { ["Value"] = 1d, ["Max"] = double.PositiveInfinity }))
    };

    foreach (var (name, render) in invalidConfigs)
    {
        try
        {
            await render();
            Require(false, $"{name} must reject its invalid configuration instead of degrading silently.");
        }
        catch (ArgumentException) { }
    }

    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        SliderContractHost host = null!;
        var root = await renderer.RenderComponentAsync<SliderContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<SliderContractHost>)(x => host = x)
        }));

        var fieldHtml = root.ToHtmlString();
        var sliderTag = Regex.Match(fieldHtml, "<div[^>]*role=\"slider\"[^>]*>").Value;
        Require(host.Field.ElementId.EndsWith("-input", StringComparison.Ordinal)
            && sliderTag.Contains($"id=\"{host.Field.ElementId}\"", StringComparison.Ordinal),
            "Slider reports and renders the FormField-adopted id.");
        Require(sliderTag.Contains("aria-labelledby=\"", StringComparison.Ordinal)
            && sliderTag.Contains("aria-describedby=\"", StringComparison.Ordinal),
            "A slider named by its field links the label and description ids.");

        var press = typeof(Slider<int>)
            .GetMethod("HandleKeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        async Task Press(string key) =>
            await (Task)press.Invoke(host.Field, [new KeyboardEventArgs { Key = key }])!;

        await Press("ArrowRight");
        Require(host.Changes.SequenceEqual([30]) && host.Value == 30,
            $"ArrowRight steps up once on a horizontal track, got [{string.Join(", ", host.Changes)}].");
        host.Changes.Clear();

        await Press("PageUp");
        Require(host.Changes.SequenceEqual([80]), "PageUp jumps ten increments at once.");
        host.Changes.Clear();

        await Press("End");
        Require(host.Changes.SequenceEqual([100]), "End goes to the maximum.");
        await Press("End");
        Require(host.Changes.SequenceEqual([100]), "End at the maximum reports no further change.");

        // RTL: the arrow step follows the direction the track visually grows in.
        await host.Field.HandleDirectionAsync(true);
        host.Changes.Clear();
        await Press("ArrowRight");
        Require(host.Changes.SequenceEqual([95]), "In RTL, ArrowRight decreases.");
        await host.Field.HandleDirectionAsync(false);

        // The vertical layout follows the APG: up/down step, left/right do not.
        host.Orientation = Orientation.Vertical;
        host.Update();
        host.Changes.Clear();
        await Press("ArrowLeft");
        Require(host.Changes.Count == 0, "Vertical sliders ignore the horizontal arrows.");
        await Press("ArrowUp");
        Require(host.Changes.SequenceEqual([95 + 5]), "Vertical sliders step up with ArrowUp.");
        host.Orientation = Orientation.Horizontal;
        host.Update();

        host.Changes.Clear();
        await host.Field.HandlePointerFractionAsync(0.333);
        Require(host.Changes.SequenceEqual([35]),
            $"A reported pointer fraction snaps to the step grid, got [{string.Join(", ", host.Changes)}] from Value={host.Value}.");
        host.Changes.Clear();
        await host.Field.HandlePointerFractionAsync(0.333);
        Require(host.Changes.Count == 0, "An unchanged pointer fraction reports nothing.");

        host.Disabled = true;
        host.Update();
        host.Changes.Clear();
        await Press("ArrowLeft");
        await host.Field.HandlePointerFractionAsync(0.5);
        Require(host.Changes.Count == 0, "Disabled sliders ignore keys and reported pointer fractions.");

        host.Disabled = false;
        host.Invalid = true;
        host.Update();
        var invalidRoot = Regex.Match(root.ToHtmlString(), "<div[^>]*role=\"slider\"[^>]*>").Value;
        Require(invalidRoot.Contains("is-invalid", StringComparison.Ordinal)
            && invalidRoot.Contains("aria-invalid=\"true\"", StringComparison.Ordinal),
            "Slider inherits the invalid state from its FormField.");

        ContinuousSliderContractHost continuous = null!;
        _ = await renderer.RenderComponentAsync<ContinuousSliderContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<ContinuousSliderContractHost>)(x => continuous = x)
        }));

        await continuous.Field.HandlePointerFractionAsync(0.333);
        Require(continuous.Changes.Count == 1 && Math.Abs(continuous.Changes[0] - 0.333d) < 0.000001d,
            $"A continuous pointer fraction must not snap, got [{string.Join(", ", continuous.Changes)}].");

        var continuousPress = typeof(Slider<double>)
            .GetMethod("HandleKeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await (Task)continuousPress.Invoke(continuous.Field, [new KeyboardEventArgs { Key = "ArrowUp" }])!;
        Require(continuous.Changes.Count == 2 && Math.Abs(continuous.Changes[1] - 0.343d) < 0.000001d,
            "A continuous arrow key moves one percent of the range.");
    });

    // Same cross-file lock as InputNumber: the JS guard must suppress exactly the
    // keys the C# keyboard model handles, and nothing else.
    var repositoryRoot = FindRepositoryRoot();
    var code = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "src/AeterniUI/Components/Slider/Slider.razor.cs"));
    var script = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "src/AeterniUI/Components/Slider/Slider.razor.js"));
    var codeKeys = KeyLiterals(code);
    Require(codeKeys.SequenceEqual(KeyLiterals(script)),
        $"The Slider key guard must match the C# keyboard model, C# has [{string.Join(", ", codeKeys)}].");
}

static string[] KeyLiterals(string text) => Regex
    .Matches(text, "[\"'](ArrowUp|ArrowDown|ArrowLeft|ArrowRight|Home|End|PageUp|PageDown)[\"']")
    .Select(match => match.Groups[1].Value)
    .Distinct()
    .OrderBy(value => value, StringComparer.Ordinal)
    .ToArray();

async Task CheckQualityStatesAsync()
{
    var tabsModule = (AeterniUI.Attributes.JsModuleAttribute)Attribute.GetCustomAttribute(
        typeof(AeterniUI.Components.Tabs.Tabs), typeof(AeterniUI.Attributes.JsModuleAttribute))!;
    Require(tabsModule.Interactive, "Tabs requires interactive module initialization before attach/focus.");
    foreach (var type in new[] { typeof(AeterniUI.Components.Button.Button), typeof(AeterniUI.Components.Surface.Surface),
        typeof(AeterniUI.Components.Input.Input), typeof(AeterniUI.Components.Popup.Popover) })
    {
        await renderer.Dispatcher.InvokeAsync(async () => {
            var root = await renderer.RenderComponentAsync(type, ParameterView.FromDictionary(new Dictionary<string, object?>
                { ["Visible"] = false, ["Style"] = "color: inherit" }));
            var html = root.ToHtmlString();
            Require(html.Contains("display: none") && html.Contains("hidden"), $"{type.Name} must enforce Visible=false over layout display rules.");
        });
    }
    await renderer.Dispatcher.InvokeAsync(async () => {
        QualityContractHost host = null!;
        var root = await renderer.RenderComponentAsync<QualityContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Ready"] = (Action<QualityContractHost>)(x => host = x) }));
        async Task Call(object component, string name, params object?[] args) =>
            await (Task)component.GetType().GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(component, args)!;
        await Call(host.Combo, "OpenAsync");
        Require(root.ToHtmlString().Contains("aria-expanded=\"true\"") && root.ToHtmlString().Contains("Empty choices"), "Empty ComboBox opens its empty content.");
        host.Disabled = host.Invalid = host.Required = true; host.Update();
        var html = root.ToHtmlString();
        var trigger = Regex.Match(html, "<button[^>]*role=\"combobox\"[^>]*>").Value;
        Require(trigger.Contains("disabled") && trigger.Contains("aria-required=\"true\"") && trigger.Contains("aria-invalid=\"true\""), "ComboBox inherits field state.");
        Require(trigger.Contains("aria-expanded=\"false\""), "Disabling an open ComboBox closes it.");
        await Call(host.Combo, "ChooseAsync", "blocked");
        Require(host.Combo.Value is null, "Disabled ComboBox rejects stale option activation.");
        var radios = Regex.Matches(html, "<input[^>]*type=\"radio\"[^>]*>");
        Require(radios.Count == 2 && radios.All(r => r.Value.Contains("disabled") && r.Value.Contains("aria-invalid=\"true\"") && r.Value.Contains("aria-required=\"true\"")), "Grouped and standalone Radio inherit field state.");
        host.HideFirst = true; host.Update(); html = root.ToHtmlString();
        Require(!html.Contains("Header A") && html.Contains("Header B"), "Hidden tab is removed from strip.");
        Require(Regex.IsMatch(html, "<div[^>]*hidden[^>]*>\\s*Panel A"), "Selected-but-hidden tab keeps its panel hidden.");
        Require(Regex.IsMatch(html, "<button[^>]*aria-selected=\"true\"[^>]*>\\s*Header B"), "Visible fallback tab is selected without mutating controlled value.");
        host.HideFirst = false; host.Update(); Require(root.ToHtmlString().Contains("Header A"), "Showing a tab refreshes strip.");
        host.ShowFirst = false; host.Update(); Require(!root.ToHtmlString().Contains("Header A"), "Removing a tab refreshes strip.");
        host.Disabled = host.Invalid = host.Required = false; host.Update();
        trigger = Regex.Match(root.ToHtmlString(), "<button[^>]*role=\"combobox\"[^>]*>").Value;
        Require(!trigger.Contains("disabled") && !trigger.Contains("aria-required"), "Field state can be cleared dynamically.");

        // REV-110: ElementId is public API that consumers turn into `for`,
        // aria-controls and aria-describedby references, so it has to be the id the
        // DOM actually carries — including when the component adopts a field id.
        var rendered = root.ToHtmlString();
        // The adopted id is what makes this discriminating: before the fix
        // ElementId reported the instance GUID while the fieldset rendered the
        // FormField's input id, so the containment check below could not pass.
        Require(host.Radios.ElementId.EndsWith("-input", StringComparison.Ordinal),
            "RadioGroup inside a FormField must report the field-adopted id from ElementId.");
        Require(rendered.Contains($"id=\"{host.Radios.ElementId}\"", StringComparison.Ordinal),
            "RadioGroup.ElementId must match the id it renders, including the FormField-adopted id.");
        Require(rendered.Contains($"id=\"{host.Standalone.ElementId}\"", StringComparison.Ordinal),
            "Radio.ElementId must match the id it renders.");
        Require(rendered.Contains($"id=\"{host.Tabs.ElementId}\"", StringComparison.Ordinal),
            "Tabs.ElementId must match the id it renders.");

        // REV-115: Rating.OnChange was removed. ValueChanged is the only callback,
        // so one interaction reports exactly one change.
        await Call(host.Rating, "SetValueAsync", 4);
        Require(host.RatingChanges.SequenceEqual([4]),
            $"Rating must report a change exactly once through ValueChanged, got [{string.Join(", ", host.RatingChanges)}].");
        Require(typeof(AeterniUI.Components.Rating.Rating).GetProperty("OnChange") is null,
            "Rating.OnChange must be gone: ValueChanged is the standard binding name.");
    });

    // REV-115: ComboBox gained the name its siblings use. The host above has no
    // items, so the confirm path is driven on a ComboBox that does.
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        var picked = new List<string>();
        var combo = await RenderComponentAsync<AeterniUI.Components.ComboBox.ComboBox<string>>(new Dictionary<string, object?>
        {
            ["Items"] = new[] { "alpha", "beta" },
            ["OnItemSelected"] = EventCallback.Factory.Create<string>(renderer, value => picked.Add(value))
        });
        await (Task)typeof(AeterniUI.Components.ComboBox.ComboBox<string>).GetMethod("ChooseAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(combo, ["beta"])!;
        Require(picked.Count == 1 && picked[0] == "beta",
            "ComboBox must report a confirmed item through OnItemSelected.");
        Require(typeof(AeterniUI.Components.ComboBox.ComboBox<string>).GetProperty("OnChange") is null,
            "ComboBox.OnChange must be gone: OnItemSelected is the name its siblings use.");
    });
    var accordion = await RenderAsync<Accordion>(new Dictionary<string, object?> {
        ["Items"] = new[] { new AccordionItem("locked", "Locked", b => b.AddContent(0, "Content"), true) },
        ["OpenKeys"] = new[] { "locked" }
    });
    Require(accordion.Contains("aria-expanded=\"false\"") && accordion.Contains("inert"), "Disabled expanded Accordion collapses rendered state without changing controlled keys.");
}

async Task CheckBreadcrumbAsync()
{
    var html = await RenderAsync<Breadcrumb>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-breadcrumb",
        ["Class"] = "consumer-breadcrumb",
        ["Separator"] = (RenderFragment)(builder => builder.AddContent(0, "/")),
        ["Items"] = new[]
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Docs", "/docs", Target: "_blank"),
            new BreadcrumbItem("Archive", "/archive", Disabled: true),
            new BreadcrumbItem("Locked", Disabled: true),
            new BreadcrumbItem("Disabled", "/disabled", Disabled: true),
            new BreadcrumbItem("Hidden", "/hidden", Visible: false),
            new BreadcrumbItem("Current")
        }
    });

    Require(html.Contains("<nav") && html.Contains("id=\"contract-breadcrumb\"") && html.Contains("consumer-breadcrumb"), "Breadcrumb preserves root attributes.");
    Require(html.Contains("aria-label=\"Breadcrumb\"") && html.Contains("aria-current=\"page\""), "Breadcrumb exposes a named navigation region and current page.");
    Require(html.Contains("<ol") && html.Contains("aeterni-breadcrumb__list"), "Breadcrumb keeps its ordered list structure for the horizontal trail.");
    Require(html.Contains("href=\"/\"") && !html.Contains("Hidden") && !html.Contains("href=\"\""), "Breadcrumb renders visible destinations as links and filters hidden items.");
    Require(html.Contains("target=\"_blank\"") && html.Contains("rel=\"noopener noreferrer\""), "Breadcrumb renders the browsing context and a safe rel for _blank destinations.");
    Require(html.Contains("href=\"/archive\"") && html.Contains("tabindex=\"-1\""), "Breadcrumb keeps disabled destinations as links outside the tab order.");
    Require(html.Split("aria-disabled=\"true\"").Length == 3, "Breadcrumb marks disabled links, not disabled plain text, with aria-disabled.");
    Require(Regex.Matches(html, "<span class=\"aeterni-breadcrumb__separator\" aria-hidden=\"true\"[^>]*>/</span>").Count == 5, "Breadcrumb renders a custom aria-hidden separator between visible items.");

    async Task RequireInvalidItems(object? invalidItems, Type exceptionType)
    {
        try
        {
            await RenderAsync<Breadcrumb>(new Dictionary<string, object?> { ["Items"] = invalidItems });
            Require(false, "Breadcrumb must reject null items and empty labels.");
        }
        catch (ArgumentException exception)
        {
            Require(exception.GetType() == exceptionType && exception.ParamName == "Items", "Breadcrumb invalid item errors must identify Items with the expected exception type.");
        }
    }

    await RequireInvalidItems(null, typeof(ArgumentNullException));
    await RequireInvalidItems(new BreadcrumbItem?[] { null }, typeof(ArgumentException));
    await RequireInvalidItems(new[] { new BreadcrumbItem(" ") }, typeof(ArgumentException));
}

#pragma warning disable BL0005 // Contract checks intentionally assign component parameters directly.
async Task CheckStepperAsync()
{
    var html = await RenderAsync<Stepper>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-stepper",
        ["Items"] = new[]
        {
            new StepperItem("one", "One", Completed: true),
            new StepperItem("two", "Two", "Current step"),
            new StepperItem("three", "Three", Disabled: true, Visible: false)
        },
        ["Value"] = "two",
        ["Orientation"] = Orientation.Vertical
    });

    Require(html.Contains("id=\"contract-stepper\"") && html.Contains("class=\"aeterni-stepper aeterni-stepper--vertical\""), "Stepper preserves root attributes and orientation.");
    Require(html.Contains("role=\"group\"") && html.Contains("aria-label=\"Progress steps\"") && html.Contains("aria-current=\"step\""), "Stepper exposes a named group and current step semantics.");
    Require(html.Contains("aria-posinset=\"2\"") && html.Contains("aria-setsize=\"2\"")
        && html.Contains("aria-label=\"Two\"") && html.Contains("Current step"),
        "Stepper reports visible position, stable step names and description relationships.");
    Require(html.Contains("is-completed") && !html.Contains("Three"), "Stepper renders explicit completion and filters hidden steps.");
    var describedButton = Regex.Match(html, "<button[^>]*aria-describedby=\"([^\"]+)\"[^>]*>").Groups[1].Value;
    Require(!string.IsNullOrEmpty(describedButton)
        && Regex.IsMatch(html, $"<span id=\"{Regex.Escape(describedButton)}\"[^>]*>Current step</span>"),
        "Stepper buttons must point aria-describedby at their rendered description.");

    var disabledHtml = await RenderAsync<Stepper>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new StepperItem("one", "One", Completed: true, Disabled: true) },
        ["Value"] = "one",
        ["Disabled"] = true,
        ["AriaLabel"] = "Checkout steps"
    });
    Require(disabledHtml.Contains("aria-label=\"Checkout steps\"")
        && disabledHtml.Contains("aria-disabled=\"true\"")
        && disabledHtml.Contains("inert")
        && Regex.IsMatch(disabledHtml, "<button[^>]*disabled")
        && disabledHtml.Contains("aria-current=\"step\""),
        "Stepper root and item disabled states must retain their native and composite semantics.");

    async Task RequireInvalidItems(object? invalidItems, Type exceptionType)
    {
        try
        {
            await RenderAsync<Stepper>(new Dictionary<string, object?> { ["Items"] = invalidItems });
            Require(false, "Stepper must reject null items and invalid item fields.");
        }
        catch (ArgumentException exception)
        {
            Require(exception.GetType() == exceptionType && exception.ParamName == "Items",
                "Stepper invalid item errors must identify Items with the expected exception type.");
        }
    }

    var customIcon = new IconDefinition("contract-step-icon", 16, 16, "M0 0h16v16H0z");
    var iconHtml = await RenderAsync<Stepper>(new Dictionary<string, object?>
    {
        ["Items"] = new[]
        {
            new StepperItem("one", "One", Completed: true, Icon: customIcon),
            new StepperItem("two", "Two", "Current step", Icon: customIcon)
        },
        ["Value"] = "two"
    });
    Require(iconHtml.Contains($"d=\"{customIcon.Paths[0]}\"") && iconHtml.Contains($"d=\"{AeterniIcons.Check.Paths[0]}\""),
        "Stepper renders a per-step marker icon and keeps the check glyph for completed steps.");

    var templatedHtml = await RenderAsync<Stepper>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new StepperItem("one", "One", "Current step"), new StepperItem("two", "Two") },
        ["Value"] = "one",
        ["ItemTemplate"] = (RenderFragment<StepperItem>)(item => builder => builder.AddContent(0, $"step-content:{item.Id}"))
    });
    Require(templatedHtml.Contains("aeterni-stepper__template")
        && templatedHtml.Contains("step-content:one")
        && templatedHtml.Contains("step-content:two")
        && templatedHtml.Contains("aria-label=\"One\""),
        "Stepper ItemTemplate renders the single content area and the accessible name for every step.");
    Require(!templatedHtml.Contains("aeterni-stepper__marker") && !templatedHtml.Contains("aria-describedby"),
        "Templated steps drop the default marker and the description relationship.");

    await RequireInvalidItems(null, typeof(ArgumentNullException));
    await RequireInvalidItems(new StepperItem?[] { null }, typeof(ArgumentException));
    await RequireInvalidItems(new[] { new StepperItem(" ", "Step") }, typeof(ArgumentException));
    await RequireInvalidItems(new[] { new StepperItem("step", " ") }, typeof(ArgumentException));
    await RequireInvalidItems(new[] { new StepperItem("step", "One"), new StepperItem("step", "Duplicate") }, typeof(ArgumentException));

    var first = new StepperItem("one", "One");
    var second = new StepperItem("two", "Two");
    var disabled = new StepperItem("disabled", "Disabled", Disabled: true);
    var component = new Stepper
    {
        Items = [first, second, disabled],
        Value = first.Id
    };
    var events = new List<string>();
    var callbackReceiver = new object();
    component.ValueChanged = EventCallback.Factory.Create<string?>(callbackReceiver, value => events.Add($"value:{value}"));
    component.OnStepClick = EventCallback.Factory.Create<StepperItem>(callbackReceiver, item => events.Add($"click:{item.Id}"));
    var select = typeof(Stepper).GetMethod("SelectAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    async Task Select(StepperItem item) => await (Task)select.Invoke(component, [item])!;

    await Select(second);
    Require(events.SequenceEqual(["value:two", "click:two"]) && component.Value == first.Id, "Stepper proposes ValueChanged before OnStepClick without mutating controlled Value.");
    events.Clear();
    await Select(first);
    await Select(disabled);
    component.Disabled = true;
    await Select(second);
    Require(events.Count == 0, "Stepper ignores current, item-disabled, and root-disabled activation.");

    var horizontalHtml = await RenderAsync<Stepper>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new StepperItem("one", "One"), new StepperItem("two", "Two") },
        ["Orientation"] = Orientation.Horizontal,
        ["AriaLabel"] = "Horizontal steps"
    });
    Require(horizontalHtml.Contains("class=\"aeterni-stepper\"") && !horizontalHtml.Contains("aeterni-stepper--vertical"),
        "Stepper horizontal rendering keeps the horizontal root class contract.");
    Require(Regex.Matches(horizontalHtml, "<li class=\"aeterni-stepper__item").Count == 2
        && horizontalHtml.Contains("aria-posinset=\"1\"")
        && horizontalHtml.Contains("aria-setsize=\"2\""),
        "Stepper horizontal rendering preserves ordered visible positions.");
}

async Task CheckTimelineAsync()
{
    var timestamp = new DateTimeOffset(2026, 10, 8, 9, 30, 0, TimeSpan.FromHours(8));
    var icon = new IconDefinition("contract-timeline-icon", 16, 16, "M0 0h16v16H0z");
    var html = await RenderAsync<Timeline>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-timeline",
        ["Class"] = "consumer-timeline",
        ["Style"] = "max-width: 30rem",
        ["AriaLabel"] = "Release history",
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-kind"] = "history" },
        ["Items"] = new[]
        {
            new TimelineItem("Created", "Build queued", timestamp, "09:30", icon, Color.Info),
            new TimelineItem("Completed", Timestamp: timestamp.AddMinutes(5), Color: Color.Success),
            new TimelineItem("Hidden", Visible: false)
        }
    });

    var root = Regex.Match(html, "<ol[^>]*id=\"contract-timeline\"[^>]*>").Value;
    Require(root.Contains("aeterni-timeline", StringComparison.Ordinal)
        && root.Contains("consumer-timeline", StringComparison.Ordinal)
        && root.Contains("aria-label=\"Release history\"", StringComparison.Ordinal)
        && root.Contains("data-kind=\"history\"", StringComparison.Ordinal)
        && root.Contains("style=\"max-width: 30rem", StringComparison.Ordinal),
        "Timeline uses a native ordered-list root and preserves common attributes.");
    Require(Regex.Matches(html, "<li class=\"aeterni-timeline__item").Count == 2
        && html.Contains("aeterni-timeline__item--info", StringComparison.Ordinal)
        && html.Contains("aeterni-timeline__item--success", StringComparison.Ordinal)
        && !html.Contains("Hidden", StringComparison.Ordinal),
        "Timeline preserves host order, semantic colours and visible filtering.");
    var encodedTimestamp = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(
        timestamp.ToString("O", CultureInfo.InvariantCulture));
    var encodedSecondTimestamp = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(
        timestamp.AddMinutes(5).ToString("O", CultureInfo.InvariantCulture));
    // REV-143: the expected value has to be encoded exactly like the rendered one.
    // ICU 72+ uses U+202F in the short time format, and Blazor renders it as
    // `&#x202F;`, so an unencoded expectation would only pass on the CI runner's
    // older ICU data while failing on any host with current data.
    var cultureTimestamp = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(
        timestamp.AddMinutes(5).ToString("g", CultureInfo.CurrentCulture));
    Require(html.Contains($"datetime=\"{encodedTimestamp}\"", StringComparison.Ordinal)
        && html.Contains(">09:30</time>", StringComparison.Ordinal)
        && html.Contains($"datetime=\"{encodedSecondTimestamp}\"", StringComparison.Ordinal)
        && html.Contains(cultureTimestamp, StringComparison.Ordinal),
        "Timeline renders machine-readable timestamps and custom or culture-formatted visible time.");
    Require(html.Contains($"d=\"{icon.Paths[0]}\"", StringComparison.Ordinal)
        && html.Contains("aeterni-timeline__dot", StringComparison.Ordinal)
        && html.Contains("Build queued", StringComparison.Ordinal),
        "Timeline renders icon and dot markers with default event copy.");
    Require(!html.Contains("<button", StringComparison.Ordinal)
        && !html.Contains("tabindex", StringComparison.OrdinalIgnoreCase)
        && !html.Contains("role=", StringComparison.OrdinalIgnoreCase),
        "Timeline remains a non-interactive native list without redundant roles or focus stops.");

    var textOnlyHtml = await RenderAsync<Timeline>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new TimelineItem("Text time", TimeText: "Yesterday") }
    });
    Require(textOnlyHtml.Contains("<span class=\"aeterni-timeline__time\"", StringComparison.Ordinal)
        && textOnlyHtml.Contains(">Yesterday</span>", StringComparison.Ordinal)
        && !textOnlyHtml.Contains("<time", StringComparison.Ordinal)
        && !textOnlyHtml.Contains("aria-label=", StringComparison.OrdinalIgnoreCase),
        "Timeline keeps text-only time labels out of the time element and leaves its optional list name unset.");

    var horizontalHtml = await RenderAsync<Timeline>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new TimelineItem("First"), new TimelineItem("Second") },
        ["Orientation"] = Orientation.Horizontal
    });
    Require(horizontalHtml.Contains("class=\"aeterni-timeline aeterni-timeline--horizontal\"", StringComparison.Ordinal)
        && Regex.Matches(horizontalHtml, "<li class=\"aeterni-timeline__item").Count == 2,
        "Timeline exposes horizontal layout without changing its ordered-list item semantics.");

    var templatedHtml = await RenderAsync<Timeline>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new TimelineItem("Original", "Default description", timestamp, "Now") },
        ["ItemTemplate"] = (RenderFragment<TimelineItem>)(item => builder => builder.AddContent(0, $"custom:{item.Title}"))
    });
    Require(templatedHtml.Contains("aeterni-timeline__template", StringComparison.Ordinal)
        && templatedHtml.Contains("custom:Original", StringComparison.Ordinal)
        && templatedHtml.Contains(">Now</time>", StringComparison.Ordinal)
        && !templatedHtml.Contains("Default description", StringComparison.Ordinal),
        "Timeline ItemTemplate replaces only default copy while retaining time and marker structure.");

    async Task RequireInvalidItems(object? invalidItems, Type exceptionType)
    {
        try
        {
            await RenderAsync<Timeline>(new Dictionary<string, object?> { ["Items"] = invalidItems });
            Require(false, "Timeline must reject null items and invalid item fields.");
        }
        catch (ArgumentException exception)
        {
            Require(exception.GetType() == exceptionType && exception.ParamName == "Items",
                "Timeline invalid item errors must identify Items with the expected exception type.");
        }
    }

    await RequireInvalidItems(null, typeof(ArgumentNullException));
    await RequireInvalidItems(new TimelineItem?[] { null }, typeof(ArgumentException));
    await RequireInvalidItems(new[] { new TimelineItem(" ") }, typeof(ArgumentException));
    await RequireInvalidItems(new[] { new TimelineItem("Invalid color", Color: (Color)99) }, typeof(ArgumentException));
}

async Task CheckDescriptionsAsync()
{
    var html = await RenderAsync<Descriptions>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-descriptions",
        ["Class"] = "consumer-descriptions",
        ["Style"] = "max-width: 40rem",
        ["Columns"] = 3,
        ["Bordered"] = true,
        ["AriaLabel"] = "Order summary",
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-kind"] = "summary" },
        ["Items"] = new[]
        {
            new DescriptionItem("Order", "AU-42"),
            new DescriptionItem("Status", "Ready"),
            new DescriptionItem("Notes", "Long text", Span: 3),
            new DescriptionItem("Owner"),
            new DescriptionItem("Hidden", "gone", Visible: false)
        }
    });

    var root = Regex.Match(html, "<dl[^>]*id=\"contract-descriptions\"[^>]*>").Value;
    Require(root.Contains("aeterni-descriptions", StringComparison.Ordinal)
        && root.Contains("aeterni-descriptions--bordered", StringComparison.Ordinal)
        && root.Contains("consumer-descriptions", StringComparison.Ordinal)
        && root.Contains("--aeterni-descriptions-columns: 3", StringComparison.Ordinal)
        && root.Contains("aria-label=\"Order summary\"", StringComparison.Ordinal)
        && root.Contains("data-kind=\"summary\"", StringComparison.Ordinal)
        && root.Contains("style=\"max-width: 40rem", StringComparison.Ordinal),
        "Descriptions uses a native description-list root and preserves common attributes.");
    Require(Regex.Matches(html, "class=\"aeterni-descriptions__item\"").Count == 4
        && html.Contains("Order", StringComparison.Ordinal)
        && html.Contains("AU-42", StringComparison.Ordinal)
        && html.Contains("grid-column: span 3", StringComparison.Ordinal)
        && !html.Contains("Hidden", StringComparison.Ordinal),
        "Descriptions filters invisible items and applies item spans.");
    Require(Regex.IsMatch(html, "<dt class=\"aeterni-descriptions__label\"[^>]*>Order</dt>")
        && Regex.IsMatch(html, "<dd class=\"aeterni-descriptions__value\"[^>]*>AU-42</dd>")
        && html.Contains("aeterni-descriptions__empty", StringComparison.Ordinal),
        "Descriptions renders label/value pairs as dt/dd and provides an empty value marker.");

    var templated = await RenderAsync<Descriptions>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new DescriptionItem("Status", "Ready") },
        ["ItemTemplate"] = (RenderFragment<DescriptionItem>)(item => builder => builder.AddContent(0, $"custom:{item.Content}"))
    });
    Require(templated.Contains("custom:Ready", StringComparison.Ordinal)
        && Regex.IsMatch(templated, "<dt class=\"aeterni-descriptions__label\"[^>]*>Status</dt>")
        && !templated.Contains("aeterni-descriptions__empty", StringComparison.Ordinal),
        "Descriptions ItemTemplate replaces only the value while retaining the label semantics.");

    foreach (var invalid in new[] { 0, 5 })
    {
        try
        {
            await RenderAsync<Descriptions>(new Dictionary<string, object?>
            {
                ["Items"] = new[] { new DescriptionItem("Value", "x") },
                ["Columns"] = invalid
            });
            Require(false, "Descriptions must reject columns outside 1..4.");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Require(exception.ParamName == "Columns", "Descriptions column errors identify Columns.");
        }
    }

    try
    {
        await RenderAsync<Descriptions>(new Dictionary<string, object?>
        {
            ["Items"] = new[] { new DescriptionItem("Value", "x", Span: 2) },
            ["Columns"] = 1
        });
        Require(false, "Descriptions must reject spans wider than the grid.");
    }
    catch (ArgumentException exception)
    {
        Require(exception.ParamName == "Items", "Descriptions span errors identify Items.");
    }
}

#pragma warning restore BL0005

async Task CheckDateBoundariesAsync()
{
    foreach (var date in new[] { DateOnly.MinValue, DateOnly.MaxValue })
    {
        await RenderAsync<DatePicker>(new Dictionary<string, object?> { ["Value"] = date });
        await RenderAsync<DateRangePicker>(new Dictionary<string, object?> { ["StartDate"] = date, ["EndDate"] = date, ["VisibleMonths"] = 3 });
        await RenderAsync<AeterniUI.Components.DateTimePicker.DateTimePicker>(new Dictionary<string, object?> { ["Value"] = date.ToDateTime(TimeOnly.MinValue) });
        await renderer.Dispatcher.InvokeAsync(async () => {
            ParameterContractHost<DateCalendar> host = null!;
            var root = await renderer.RenderComponentAsync<ParameterContractHost<DateCalendar>>(ParameterView.FromDictionary(new Dictionary<string, object?> {
                ["Ready"] = (Action<ParameterContractHost<DateCalendar>>)(x => {
                    host = x; x.Values = new() { ["DisplayMonth"] = date, ["SelectedDate"] = date, ["VisibleMonths"] = 3 };
                })
            }));
            var keyboard = typeof(DateCalendar).GetMethod("HandleKeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            foreach (var key in new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End", "PageUp", "PageDown" })
                await (Task)keyboard.Invoke(host.Inner, [new KeyboardEventArgs { Key = key }, date])!;
            var html = root.ToHtmlString();
            Require(Regex.Matches(html, "role=\"gridcell\"").Count % 42 == 0, "Boundary calendars preserve complete 6x7 grids with inert blank cells.");
        });
    }
    await RenderAsync<DateRangePicker>(new Dictionary<string, object?> {
        ["Presets"] = new[] { new AeterniUI.Models.DateRangePreset("Last day", DateOnly.MaxValue, DateOnly.MaxValue) }
    });
}

async Task CheckOverlayTransitionsAsync()
{
    await Check<AeterniUI.Components.Drawer.Drawer>();
    await Check<AeterniUI.Components.Popup.Popover>();
    async Task Check<T>() where T : IComponent => await renderer.Dispatcher.InvokeAsync(async () => {
        ParameterContractHost<T> host = null!;
        var root = await renderer.RenderComponentAsync<ParameterContractHost<T>>(ParameterView.FromDictionary(new Dictionary<string, object?> {
            ["Ready"] = (Action<ParameterContractHost<T>>)(x => { host = x; x.Values["Open"] = true; })
        }));
        host.Values["OpenChanged"] = EventCallback.Factory.Create<bool>(host, (bool _) => { });
        host.Update();
        await (Task)typeof(T).GetMethod("RequestCloseAsync")!.Invoke(host.Inner, null)!;
        Require((bool)typeof(T).GetProperty("Open")!.GetValue(host.Inner)!, "Controlled consumer may reject close without parameter mutation.");
        host.Values["Open"] = false; host.Update();
        Require(root.ToHtmlString().Contains("is-closing"), $"{typeof(T).Name} keeps exit chrome while closing.");
        var revision = (int)typeof(T).GetField("_closeRevision", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Inner)!;
        host.Values["Open"] = true; host.Update();
        await (Task)typeof(T).GetMethod("FinalizeCloseAsync")!.Invoke(host.Inner, [revision])!;
        Require(!root.ToHtmlString().Contains("is-closing"), "Stale exit completion cannot hide reopened overlay.");
        host.Values["Open"] = false; host.Update();
        revision = (int)typeof(T).GetField("_closeRevision", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(host.Inner)!;
        await (Task)typeof(T).GetMethod("FinalizeCloseAsync")!.Invoke(host.Inner, [revision])!;
        Require(root.ToHtmlString().Contains("hidden") && !root.ToHtmlString().Contains("is-closing"), "Completed exit hides overlay.");
    });
}

void CheckTimeMapReuse()
{
    var type = typeof(TimePicker).Assembly.GetType("AeterniUI.Components.TimePicker.TimePickerOptions")!;
    var factory = type.GetMethod("CreateMap", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
    object Map(TimeSpan step, TimeOnly? min, TimeOnly? max, Func<TimeOnly, bool>? disabled = null, object? previous = null) =>
        factory.Invoke(null, [step, min, max, disabled, previous])!;
    bool Contains(object map, TimeOnly time) => (bool)map.GetType().GetMethod("Contains", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(map, [time])!;
    var step = TimeSpan.FromSeconds(1);
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var full = Map(step, null, null); var cold = timer.Elapsed.TotalMilliseconds;
    timer.Restart();
    for (var i = 0; i < 100; i++) Require(ReferenceEquals(full, Map(step, null, null, previous: full)), "Unchanged predicate-free maps reuse instance.");
    Console.WriteLine($"Time map profile: cold={cold:F2}ms; 100 reused={timer.Elapsed.TotalMilliseconds:F2}ms (informational).");
    Require(!ReferenceEquals(full, Map(step, new TimeOnly(9, 0), null, previous: full)), "Bounds invalidate map reuse.");
    bool blocked = false;
    Func<TimeOnly, bool> predicate = _ => blocked;
    var first = Map(step, new TimeOnly(9, 0), new TimeOnly(9, 1), predicate); blocked = true;
    var next = Map(step, new TimeOnly(9, 0), new TimeOnly(9, 1), predicate, first);
    Require(!ReferenceEquals(first, next) && !Contains(next, new TimeOnly(9, 0)), "Mutable delegate closures are recomputed.");
    foreach (var seconds in new[] { 1, 7, 60, 3601 })
    {
        var min = new TimeOnly(8, 10, 20).Add(TimeSpan.FromTicks(1)); var max = new TimeOnly(8, 12, 40);
        var map = Map(TimeSpan.FromSeconds(seconds), min, max);
        for (var second = 0; second < 86400; second++)
        {
            var time = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(second));
            if (Contains(map, time) != (second % seconds == 0 && time >= min && time <= max))
            { Require(false, $"Bounded map differs from full enumeration at {time}, step {seconds}."); break; }
        }
    }
    var finalFraction = Map(step, TimeOnly.MaxValue, TimeOnly.MaxValue);
    Require(!Contains(finalFraction, new TimeOnly(23, 59, 59)), "Fractional minimum after final whole second yields no candidate.");
}

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
    args["MaxHeight"] = "220px";
    html = await RenderAsync<AeterniUI.Components.List.List<string?>>(args);
    Require(html.Contains("aeterni-list--scrollable") && html.Contains("--aeterni-list-max-height: 220px"), "Bounded lists must expose their own scroll viewport size.");
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
        Task Key(string key) => host.List.HandleKeyFromBrowserAsync(key,
            Regex.Matches(root.ToHtmlString(), "id=\"([^\"]+)\"[^>]*role=\"option\"")
                .Select(match => match.Groups[1].Value).ToArray());
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
        host.Rows = ["Alpha", "Beta", "Gamma"]; host.RowDisabled = false; host.Keyed = true; host.AllowClear = true; host.Update();
        await Key("Home"); await Key("Home");
        Require(Equals(host.Selected, "Alpha"), "Repeated Home preserves selection even when AllowClear is enabled.");
        await Key("Enter");
        Require(host.Selected is null, "Explicit activation can still clear a single selection.");
        await Key("Home"); host.HiddenRow = "Alpha"; host.Update();
        Require(!root.ToHtmlString().Contains("aria-activedescendant"), "Hiding the active row clears its active descendant.");
        await Key("Home");
        Require(Equals(host.Selected, "Beta"), "Hidden rows must be skipped by keyboard navigation.");
        host.HiddenRow = null; host.Rows = ["Gamma", "Beta", "Alpha"]; host.Update();
        var ids = Regex.Matches(root.ToHtmlString(), "id=\"([^\"]+)\"[^>]*role=\"option\"")
            .Select(match => match.Groups[1].Value).ToArray();
        Require(ids.Length == 3, "Keyed reorder exposes three DOM option ids.");
        await host.List.HandleKeyFromBrowserAsync("Home", ids);
        Require(Equals(host.Selected, "Gamma"), "Home follows actual DOM order after keyed reordering.");
        await host.List.HandleKeyFromBrowserAsync("End", ids);
        Require(Equals(host.Selected, "Alpha"), "End follows actual DOM order after keyed reordering.");
        host.Rows = ["Alpha"]; host.Update(); await Key("Home"); await Key("ArrowDown");
        Require(Equals(host.Selected, "Alpha"), "Wrapping a one-item list must not clear selection.");
    });
}

async Task CheckVirtualListAsync()
{
    var items = Enumerable.Range(1, 100).Select(index => $"Item {index}").ToArray();
    var html = await RenderAsync<VirtualList<string>>(new Dictionary<string, object?>
    {
        [nameof(VirtualList<string>.Items)] = items,
        [nameof(VirtualList<string>.Height)] = "240px",
        [nameof(VirtualList<string>.ItemSize)] = 32d,
        [nameof(VirtualList<string>.OverscanCount)] = 4,
        [nameof(VirtualList<string>.SelectionMode)] = SelectionMode.Single,
        [nameof(VirtualList<string>.AriaLabel)] = "Virtual items"
    });

    foreach (var expected in new[] { "aeterni-virtual-list", "role=\"listbox\"", "aria-multiselectable=\"false\"", "--aeterni-virtual-list-height: 240px", "--aeterni-virtual-list-item-size: 32px" })
    {
        Require(html.Contains(expected, StringComparison.Ordinal), $"VirtualList must expose {expected}.");
    }

    Require(!html.Contains("aria-setsize=", StringComparison.Ordinal), "VirtualList must not place option position metadata on the listbox root before browser virtualization mounts rows.");

    var loading = await RenderAsync<VirtualList<string>>(new Dictionary<string, object?>
    {
        [nameof(VirtualList<string>.Items)] = items,
        [nameof(VirtualList<string>.HasMoreItems)] = true,
        [nameof(VirtualList<string>.LoadingMore)] = true,
        [nameof(VirtualList<string>.LoadMoreThreshold)] = 48d,
        [nameof(VirtualList<string>.OnLoadMore)] = EventCallback.Factory.Create(renderer, () => Task.CompletedTask)
    });
    Require(loading.Contains("aria-busy=\"true\"", StringComparison.Ordinal), "VirtualList must expose aria-busy while loading more items.");

    var plain = await RenderAsync<VirtualList<string>>(new Dictionary<string, object?>
    {
        [nameof(VirtualList<string>.Items)] = Array.Empty<string>(),
        [nameof(VirtualList<string>.EmptyContent)] = (RenderFragment)(builder => builder.AddContent(0, "Empty virtual list"))
    });
    Require(plain.Contains("Empty virtual list", StringComparison.Ordinal) && plain.Contains("role=\"list\"", StringComparison.Ordinal), "VirtualList empty display mode must retain plain list semantics.");

    // Each scenario gets its own host: the active row is component state, so
    // sharing one host would let an earlier scenario steer a later assertion.
    async Task<VirtualListContractHost> HostAsync(Action<VirtualListContractHost>? configure = null)
    {
        VirtualListContractHost created = null!;
        await renderer.RenderComponentAsync<VirtualListContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["Ready"] = (Action<VirtualListContractHost>)(x => created = x) }));
        configure?.Invoke(created);
        created.Update();
        return created;
    }

    async Task Call(VirtualListContractHost host, string name, params object?[] args) =>
        await (Task)typeof(VirtualList<string>).GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!.Invoke(host.List, args)!;

    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        // Keyboard navigation moves the active row and commits it in single mode,
        // so the browser-facing entry point is exercised, not just its markup.
        var single = await HostAsync();
        await Call(single, "HandleKeyFromBrowserAsync", "ArrowDown");
        Require(Equals(single.Selected, "Alpha"), "VirtualList ArrowDown must activate and select the first row.");
        Require(single.Picked.Count == 1 && single.Picked[0] == "Alpha", "VirtualList must report the picked row through OnItemSelected.");
        await Call(single, "HandleKeyFromBrowserAsync", "End");
        Require(Equals(single.Selected, "Gamma"), "VirtualList End must jump to the last row.");
        await Call(single, "HandleKeyFromBrowserAsync", "Home");
        Require(Equals(single.Selected, "Alpha"), "VirtualList Home must return to the first row.");

        // A row the host disables is skipped rather than activated.
        var skipping = await HostAsync(x => x.DisabledSelector = value => value == "Beta");
        await Call(skipping, "HandleKeyFromBrowserAsync", "ArrowDown");
        Require(Equals(skipping.Selected, "Alpha"), "VirtualList must start on the first enabled row.");
        await Call(skipping, "HandleKeyFromBrowserAsync", "ArrowDown");
        Require(Equals(skipping.Selected, "Gamma"), "VirtualList must skip rows disabled by DisabledSelector.");

        // Multi-select separates navigation from selection: arrows move the active
        // row and only Enter/Space toggle it into the set, so a stray arrow key
        // cannot rewrite a multi-row selection.
        var multiple = await HostAsync(x => x.Mode = SelectionMode.Multiple);
        await Call(multiple, "HandleKeyFromBrowserAsync", "ArrowDown");
        Require(multiple.SelectedMany.Count == 0, "An arrow key must not select in multi-select mode.");
        await Call(multiple, "HandleKeyFromBrowserAsync", "Enter");
        await Call(multiple, "HandleKeyFromBrowserAsync", "ArrowDown");
        await Call(multiple, "HandleKeyFromBrowserAsync", "Enter");
        Require(multiple.SelectedMany.Count == 2, "VirtualList multi-select must accumulate rows toggled with Enter.");

        // The in-flight latch is a concurrency guard: a second request raised while
        // the host is still answering the first must not reach the host again.
        var gated = await HostAsync(x => { x.HasMoreItems = true; x.LoadMoreGate = new TaskCompletionSource(); });
        var firstLoad = Call(gated, "HandleLoadMoreAsync");
        await Call(gated, "HandleLoadMoreAsync");
        Require(gated.LoadMoreCalls == 1, "VirtualList must not raise a second load while one is in flight.");
        gated.LoadMoreGate!.SetResult();
        await firstLoad;
        Require(gated.LoadMoreCalls == 1, "VirtualList must not report the same load twice.");

        // Once the host re-arms it, a fresh request goes through again.
        gated.LoadMoreGate = null;
        await Call(gated, "HandleLoadMoreAsync");
        Require(gated.LoadMoreCalls == 2, "VirtualList must report again once the host re-arms it.");

        // A key selector that is not unique cannot address rows, so it must fail loudly.
        foreach (var selector in new Func<string, object?>[] { _ => "same", _ => null })
        {
            try
            {
                await HostAsync(x => x.KeySelector = selector);
                Require(false, "VirtualList must reject an ItemKeySelector that is not a unique non-null key.");
            }
            catch (ArgumentException) { }
        }

        // REV-107: the browser can queue a callback that lands after the component
        // is gone. Driving a real instance matters here — a bare one returns early
        // on IsSelectable and would pass without proving the guard exists.
        var disposed = await HostAsync();
        await Call(disposed, "HandleKeyFromBrowserAsync", "ArrowDown");
        Require(Equals(disposed.Selected, "Alpha"), "VirtualList must select before disposal, so the guard below is meaningful.");

        await ((IAsyncDisposable)disposed.List).DisposeAsync();

        disposed.Selected = null;
        disposed.Picked.Clear();
        await Call(disposed, "HandleKeyFromBrowserAsync", "ArrowDown");
        await Call(disposed, "HandleKeyFromBrowserAsync", "Enter");
        Require(disposed.Selected is null && disposed.Picked.Count == 0,
            "VirtualList must ignore a queued browser callback that arrives after disposal.");
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

async Task CheckSearchAsync()
{
    var html = await RenderAsync<Search>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-search",
        ["Class"] = "consumer-search",
        ["Style"] = "margin: 0",
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "search" },
        ["Value"] = "button",
        ["Size"] = Size.Small,
        ["Required"] = true,
        ["Invalid"] = true,
        ["Disabled"] = true,
        ["Loading"] = true,
        ["AriaLabel"] = "Component search",
        ["SubmitLabel"] = "Run search",
        ["ClearLabel"] = "Clear component search"
    });

    var root = Regex.Match(html, "^<div[^>]*>").Value;
    foreach (var value in new[]
    {
        "id=\"contract-search\"", "consumer-search", "margin: 0", "data-contract=\"search\"",
        "role=\"search\"", "aria-label=\"Component search\"", "aria-disabled=\"true\"", "aria-busy=\"true\"",
        "aeterni-search--sm", "is-invalid", "is-disabled", "is-loading"
    })
    {
        Require(root.Contains(value), $"Search root must render {value}.");
    }

    var input = Regex.Match(html, "<input[^>]*>").Value;
    Require(input.Contains("type=\"text\"") && input.Contains("disabled")
        && input.Contains("aria-required=\"true\"") && input.Contains("aria-invalid=\"true\""),
        "Search must forward search input semantics and field state.");
    Require(html.Contains("aria-label=\"Run search\"") && html.Contains("aria-label=\"Clear component search\""),
        "Search must render accessible labels for its built-in submit and clear actions.");
    Require(html.Contains("role=\"group\"")
        && html.Contains("aeterni-search__submit")
        && html.Contains("aeterni-search__clear")
        && html.Contains("aeterni-icon-button--sm"),
        "Search shows compact submit and clear actions inside the input.");

    var empty = await RenderAsync<Search>(new Dictionary<string, object?> { ["Value"] = string.Empty });
    Require(!empty.Contains("aeterni-search__clear"), "Search hides the clear action for an empty value.");

    var notClearable = await RenderAsync<Search>(new Dictionary<string, object?>
    {
        ["Value"] = "button", ["Clearable"] = false
    });
    Require(!notClearable.Contains("aeterni-search__clear")
        && notClearable.Contains("aeterni-search__submit"),
        "Search Clearable=false disables only its clear action and retains the built-in submit action.");

    var loading = await RenderAsync<Search>(new Dictionary<string, object?>
    {
        ["Value"] = "button", ["Loading"] = true, ["Size"] = Size.Large
    });
    var loadingInput = Regex.Match(loading, "<input[^>]*>").Value;
    Require(loading.Contains("aria-disabled=\"true\"") && loadingInput.Contains("disabled")
        && loading.Contains("aeterni-icon-button--lg"),
        "Search loading state must disable the whole composite and size both actions with the input.");

    foreach (var size in Enum.GetValues<Size>())
    {
        var sized = await RenderAsync<Search>(new Dictionary<string, object?> { ["Size"] = size });
        Require(sized.Contains("aeterni-search--sm") == (size == Size.Small)
            && sized.Contains("aeterni-search--lg") == (size == Size.Large),
            $"Search {size} must render only its matching size modifier.");
    }
}

async Task CheckAutocompleteAsync()
{
    var html = await RenderAsync<Autocomplete<string>>(new Dictionary<string, object?>
    {
        ["Id"] = "contract-autocomplete",
        ["Class"] = "consumer-autocomplete",
        ["Style"] = "margin: 0",
        ["AdditionalAttributes"] = new Dictionary<string, object> { ["data-contract"] = "autocomplete" },
        ["Value"] = "bu",
        ["Items"] = new[] { "Button", "Breadcrumb" },
        ["Size"] = Size.Small,
        ["Required"] = true,
        ["Invalid"] = true,
        ["Disabled"] = true,
        ["Loading"] = true,
        ["AriaLabel"] = "Component suggestions",
        ["ClearLabel"] = "Clear component suggestions"
    });

    var root = Regex.Match(html, "<div[^>]*id=\"contract-autocomplete\"[^>]*>").Value;
    foreach (var value in new[]
    {
        "id=\"contract-autocomplete\"", "consumer-autocomplete", "margin: 0", "data-contract=\"autocomplete\"",
        "aria-disabled=\"true\"", "aria-busy=\"true\"", "aeterni-autocomplete--sm", "is-invalid", "is-disabled", "is-loading"
    })
    {
        Require(root.Contains(value), $"Autocomplete root must render {value}.");
    }

    var input = Regex.Match(html, "<input[^>]*>").Value;
    Require(input.Contains("type=\"text\"") && input.Contains("role=\"combobox\"")
        && input.Contains("aria-expanded=\"false\"") && input.Contains("aria-haspopup=\"listbox\"")
        && input.Contains("aria-autocomplete=\"list\"") && input.Contains("aria-required=\"true\"")
        && input.Contains("aria-invalid=\"true\"") && input.Contains("disabled"),
        "Autocomplete must expose combobox and field state semantics on its input.");
    Require(html.Contains("role=\"listbox\"")
        && html.Contains("role=\"group\"")
        && html.Contains("aeterni-autocomplete__clear")
        && html.Contains("aeterni-icon-button--sm")
        && html.Contains("aria-label=\"Clear component suggestions\""),
        "Autocomplete must render its listbox and compact clear action.");
    Require(html.Contains("Loading suggestions"), "Autocomplete exposes its loading status in the suggestion panel.");

    var notClearable = await RenderAsync<Autocomplete<string>>(new Dictionary<string, object?>
    {
        ["Value"] = "bu", ["Items"] = new[] { "Button" }, ["Clearable"] = false
    });
    Require(!notClearable.Contains("aeterni-autocomplete__clear"), "Autocomplete Clearable=false disables its clear action.");

    var loading = await RenderAsync<Autocomplete<string>>(new Dictionary<string, object?>
    {
        ["Value"] = "bu", ["Items"] = new[] { "Button" }, ["Loading"] = true, ["Size"] = Size.Large
    });
    var loadingInput = Regex.Match(loading, "<input[^>]*>").Value;
    Require(loading.Contains("aria-disabled=\"true\"") && loadingInput.Contains("disabled")
        && loading.Contains("aeterni-icon-button--lg"),
        "Autocomplete loading state must disable the whole composite and size the clear action with the input.");

    var suggestions = await RenderAsync<Autocomplete<string>>(new Dictionary<string, object?>
    {
        ["Value"] = "bu",
        ["Items"] = new[] { "Button", "Breadcrumb" }
    });
    Require(suggestions.Contains("role=\"option\"") && suggestions.Contains("Button"),
        "Autocomplete renders host-provided suggestion options.");

    var empty = await RenderAsync<Autocomplete<string>>(new Dictionary<string, object?>
    {
        ["Items"] = Array.Empty<string>(),
        ["AriaLabel"] = "Suggestions"
    });
    Require(empty.Contains("No suggestions"), "Autocomplete renders a default empty suggestion message.");

    try
    {
        await RenderAsync<Autocomplete<string>>(new Dictionary<string, object?> { ["Size"] = (Size)99 });
        Require(false, "Autocomplete must reject unknown sizes.");
    }
    catch (ArgumentOutOfRangeException) { }
}

async Task CheckMultiSelectAsync()
{
    var html = await RenderAsync<MultiSelect<string>>(new Dictionary<string, object?>
    {
        [nameof(MultiSelect<string>.Id)] = "contract-multi-select",
        [nameof(MultiSelect<string>.Class)] = "consumer-multi-select",
        [nameof(MultiSelect<string>.Style)] = "margin: 0",
        [nameof(MultiSelect<string>.AdditionalAttributes)] = new Dictionary<string, object> { ["data-contract"] = "multi-select" },
        [nameof(MultiSelect<string>.Items)] = new[] { "Design", "Development", "Operations" },
        [nameof(MultiSelect<string>.SelectedValues)] = new[] { "Design", "Operations" },
        [nameof(MultiSelect<string>.Size)] = Size.Small,
        [nameof(MultiSelect<string>.AllowSelectAll)] = true,
        [nameof(MultiSelect<string>.AllowClear)] = true,
        [nameof(MultiSelect<string>.Required)] = true,
        [nameof(MultiSelect<string>.Invalid)] = true,
        [nameof(MultiSelect<string>.AriaLabel)] = "Component categories"
    });

    var root = Regex.Match(html, "<div[^>]*id=\"contract-multi-select\"[^>]*>").Value;
    foreach (var value in new[]
    {
        "id=\"contract-multi-select\"", "consumer-multi-select", "margin: 0", "data-contract=\"multi-select\"",
        "aeterni-multi-select--sm", "is-invalid"
    })
    {
        Require(root.Contains(value), $"MultiSelect root must render {value}.");
    }

    var trigger = Regex.Match(html, "<div[^>]*role=\"combobox\"[^>]*>").Value;
    Require(trigger.Contains("aria-expanded=\"false\"")
        && trigger.Contains("aria-haspopup=\"listbox\"")
        && trigger.Contains("aria-required=\"true\"")
        && trigger.Contains("aria-invalid=\"true\"")
        && trigger.Contains("aria-label=\"Component categories\""),
        "MultiSelect must expose combobox and field state semantics on its trigger.");
    Require(Regex.Matches(html, "class=\"aeterni-multi-select__chip\"").Count == 2
        && html.Contains("Remove Design")
        && html.Contains("Remove Operations"),
        "MultiSelect must render selected values as removable chips.");
    Require(html.Contains("class=\"aeterni-multi-select__host aeterni-popup-host\"")
        && html.Contains("width: min(100%, var(--aeterni-width-input-md));"),
        "MultiSelect popup host must stay aligned with the trigger width.");
    Require(!html.Contains("}"), "MultiSelect must not leak Razor closing braces into rendered markup.");
    // Built-in defaults must be English: a host that never overrides Text has to
    // render a usable label, and the bulk actions paint this value as visible
    // button text, not only as an accessible name.
    Require(html.Contains(">Select all<") && html.Contains(">Clear<"),
        "MultiSelect default bulk actions must use the built-in English labels.");
    Require(!Regex.IsMatch(html, ">[^<]*[\\u4e00-\\u9fff][^<]*<"),
        "MultiSelect must not render CJK text from built-in defaults.");

    var empty = await RenderAsync<MultiSelect<string>>(new Dictionary<string, object?>
    {
        [nameof(MultiSelect<string>.Items)] = Array.Empty<string>(),
        [nameof(MultiSelect<string>.AriaLabel)] = "Categories"
    });
    Require(empty.Contains("Select options"), "MultiSelect renders its default placeholder.");

    foreach (var size in Enum.GetValues<Size>())
    {
        var sized = await RenderAsync<MultiSelect<string>>(new Dictionary<string, object?> { [nameof(MultiSelect<string>.Size)] = size });
        Require(sized.Contains("aeterni-multi-select--sm") == (size == Size.Small)
            && sized.Contains("aeterni-multi-select--lg") == (size == Size.Large),
            $"MultiSelect {size} must render only its matching size modifier.");
    }

    try
    {
        await RenderAsync<MultiSelect<string>>(new Dictionary<string, object?> { [nameof(MultiSelect<string>.Size)] = (Size)99 });
        Require(false, "MultiSelect must reject unknown sizes.");
    }
    catch (ArgumentOutOfRangeException) { }
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

async Task CheckFlashCardAsync()
{
    var module = (AeterniUI.Attributes.JsModuleAttribute)Attribute.GetCustomAttribute(
        typeof(FlashCard), typeof(AeterniUI.Attributes.JsModuleAttribute))!;
    Require(module.Name == "flash-card", "FlashCard must declare its pointer following module.");

    RenderFragment back = builder => builder.AddContent(0, "Back side");

    var flippable = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.ImageSrc)] = "/cover.svg",
        [nameof(FlashCard.ImageAlt)] = "Cover",
        [nameof(FlashCard.Title)] = "Cover title",
        [nameof(FlashCard.Back)] = back
    });

    Require(flippable.Contains("role=\"button\"", StringComparison.Ordinal)
        && flippable.Contains("aria-pressed=\"false\"", StringComparison.Ordinal)
        && flippable.Contains("tabindex=\"0\"", StringComparison.Ordinal),
        "A flippable FlashCard must expose button semantics and its pressed state.");
    Require(flippable.Contains("aria-label=\"Cover title\"", StringComparison.Ordinal), "FlashCard must fall back to Title as its accessible name.");
    Require(flippable.Contains("is-flippable", StringComparison.Ordinal), "A flippable FlashCard must advertise its pointer cursor class.");
    Require(flippable.Contains("alt=\"Cover\"", StringComparison.Ordinal), "FlashCard must render the supplied alternative text.");
    Require(flippable.Contains("--aeterni-flash-card-perspective: 800px", StringComparison.Ordinal)
        && flippable.Contains("--aeterni-flash-card-scale: 1.02", StringComparison.Ordinal),
        "Tilt parameters must reach the isolated stylesheet through the card's custom properties.");
    Require(flippable.Contains("aeterni-flash-card--sheen-holo", StringComparison.Ordinal)
        && flippable.Contains("aeterni-flash-card__sheen", StringComparison.Ordinal)
        && flippable.Contains("--aeterni-flash-card-sheen-opacity: 0.6", StringComparison.Ordinal),
        "The default finish must render the holographic overlay with its strength token.");
    Require(flippable.Contains("aeterni-flash-card__sheen-light", StringComparison.Ordinal),
        "The holographic finish must render its white reflection on a second layer.");

    var hiddenBackTag = Regex.Match(flippable, "<div[^>]*aeterni-flash-card__face--back[^>]*>").Value;
    Require(hiddenBackTag.Contains("aria-hidden=\"true\"", StringComparison.Ordinal) && hiddenBackTag.Contains("inert", StringComparison.Ordinal),
        "The back face of an unflipped FlashCard must leave the accessibility tree.");
    var frontTag = Regex.Match(flippable, "<div[^>]*aeterni-flash-card__face--front[^>]*>").Value;
    Require(frontTag.Length > 0 && !frontTag.Contains("aria-hidden", StringComparison.Ordinal) && !frontTag.Contains("inert", StringComparison.Ordinal),
        "The visible front face must stay exposed while the card is unflipped.");

    var flipped = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.ImageSrc)] = "/cover.svg",
        [nameof(FlashCard.Back)] = back,
        [nameof(FlashCard.IsFlipped)] = true
    });

    Require(flipped.Contains("aria-pressed=\"true\"", StringComparison.Ordinal) && flipped.Contains("is-flipped", StringComparison.Ordinal),
        "IsFlipped must reach both the rendered state and the pressed state.");
    frontTag = Regex.Match(flipped, "<div[^>]*aeterni-flash-card__face--front[^>]*>").Value;
    Require(frontTag.Contains("aria-hidden=\"true\"", StringComparison.Ordinal) && frontTag.Contains("inert", StringComparison.Ordinal),
        "The front face must leave the accessibility tree once the card shows its back.");
    var visibleBackTag = Regex.Match(flipped, "<div[^>]*aeterni-flash-card__face--back[^>]*>").Value;
    Require(!visibleBackTag.Contains("aria-hidden", StringComparison.Ordinal) && !visibleBackTag.Contains("inert", StringComparison.Ordinal),
        "The visible back face must stay exposed while the card is flipped.");

    var staticCard = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.Title)] = "Static card"
    });

    Require(!staticCard.Contains("role=\"button\"", StringComparison.Ordinal) && !staticCard.Contains("tabindex", StringComparison.Ordinal),
        "A FlashCard without Back content must stay a static container.");
    Require(!staticCard.Contains("aeterni-flash-card__face--back", StringComparison.Ordinal), "A static FlashCard must not render a back face.");
    Require(!staticCard.Contains("aeterni-flash-card__sheen", StringComparison.Ordinal), "A card without media has no surface for the finish overlay.");

    var imageOnly = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.ImageSrc)] = "/cover.svg",
        [nameof(FlashCard.ImageAlt)] = "Cover"
    });

    Require(imageOnly.Contains("aeterni-flash-card__media", StringComparison.Ordinal)
        && !imageOnly.Contains("aeterni-flash-card__caption", StringComparison.Ordinal),
        "An image only FlashCard must render its media without a caption area.");
    Require(!imageOnly.Contains("role=\"button\"", StringComparison.Ordinal), "An image only FlashCard is not flippable.");

    var noSheen = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.ImageSrc)] = "/cover.svg",
        [nameof(FlashCard.Sheen)] = FlashCardSheen.None
    });

    Require(!noSheen.Contains("aeterni-flash-card__sheen", StringComparison.Ordinal)
        && !noSheen.Contains("--sheen-holo", StringComparison.Ordinal),
        "Sheen=None must render no finish overlay at all.");

    var shineSheen = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.ImageSrc)] = "/cover.svg",
        [nameof(FlashCard.Sheen)] = FlashCardSheen.Shine
    });

    Require(shineSheen.Contains("aeterni-flash-card--sheen-shine", StringComparison.Ordinal)
        && shineSheen.Contains("aeterni-flash-card__sheen", StringComparison.Ordinal)
        && !shineSheen.Contains("aeterni-flash-card__sheen-light", StringComparison.Ordinal),
        "The shine finish keeps its single screen layer; only holo needs the white-light layer.");

    var hoverPreview = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.Back)] = back,
        [nameof(FlashCard.FlipTrigger)] = FlashCardFlipTrigger.Hover
    });

    Require(hoverPreview.Contains("is-flip-hover", StringComparison.Ordinal), "The Hover trigger must emit its public modifier class.");
    Require(!hoverPreview.Contains("role=\"button\"", StringComparison.Ordinal)
        && !hoverPreview.Contains("tabindex", StringComparison.Ordinal)
        && !hoverPreview.Contains("aria-pressed", StringComparison.Ordinal),
        "A hover preview is pointer-only and must not present itself as a control.");
    Require(!hoverPreview.Contains("aria-hidden", StringComparison.Ordinal) && !hoverPreview.Contains("inert", StringComparison.Ordinal),
        "A hover preview keeps both faces readable, because the browser owns the flipped state.");

    var disabled = await RenderAsync<FlashCard>(new Dictionary<string, object?>
    {
        [nameof(FlashCard.Back)] = back,
        [nameof(FlashCard.Title)] = "Disabled card",
        [nameof(FlashCard.Disabled)] = true
    });

    Require(disabled.Contains("aria-disabled=\"true\"", StringComparison.Ordinal) && !disabled.Contains("tabindex", StringComparison.Ordinal),
        "A disabled FlashCard must announce its state and leave the tab sequence.");

    const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var onParametersSet = typeof(FlashCard).GetMethod("OnParametersSet", flags)!;
    // Parameters are assigned reflectively so the contract host stays a plain
    // consumer without triggering the BL0005 analyzer.
    var invalidRatio = new FlashCard();
    typeof(FlashCard).GetProperty(nameof(FlashCard.ImageSrc))!.SetValue(invalidRatio, "/cover.svg");
    typeof(FlashCard).GetProperty(nameof(FlashCard.AspectRatio))!.SetValue(invalidRatio, 0d);
    Require(Throws(onParametersSet, invalidRatio), "AspectRatio must reject non-positive values.");
    Require(Throws(onParametersSet, new FlashCard()), "A FlashCard without any content must be rejected.");

    var saturatedSheen = new FlashCard();
    typeof(FlashCard).GetProperty(nameof(FlashCard.ImageSrc))!.SetValue(saturatedSheen, "/cover.svg");
    typeof(FlashCard).GetProperty(nameof(FlashCard.SheenIntensity))!.SetValue(saturatedSheen, 1.5d);
    Require(Throws(onParametersSet, saturatedSheen), "SheenIntensity must reject values above 1.");

    var root = FindRepositoryRoot();
    var css = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/FlashCard/FlashCard.razor.css"));
    Require(css.Contains("perspective: var(--aeterni-flash-card-perspective)", StringComparison.Ordinal), "FlashCard must expose the perspective token of its tilt stage.");
    Require(css.Contains(".aeterni-flash-card.is-flipped .aeterni-flash-card__inner", StringComparison.Ordinal), "The flipped state must have a matching style rule.");
    Require(css.Contains(".aeterni-flash-card.is-flip-hover:not(.is-disabled):hover .aeterni-flash-card__inner", StringComparison.Ordinal),
        "The Hover trigger must have a matching style rule.");
    Require(css.Contains(".aeterni-flash-card--sheen-shine .aeterni-flash-card__sheen", StringComparison.Ordinal)
        && css.Contains(".aeterni-flash-card--sheen-holo .aeterni-flash-card__sheen", StringComparison.Ordinal),
        "Both finishes need their own sheen rule.");
    // REV-140: the rainbow replaces hue and saturation (`color`) while the white
    // reflection lightens the artwork (`screen`). A single `screen` layer made the
    // finish invisible on light artwork, so the two roles must stay split.
    Require(Regex.IsMatch(css, @"--sheen-holo \.aeterni-flash-card__sheen \{[^}]*mix-blend-mode: color", RegexOptions.Singleline),
        "The holo rainbow must blend with `color` so it keeps the artwork's own luminosity.");
    Require(Regex.IsMatch(css, @"--sheen-holo \.aeterni-flash-card__sheen-light \{[^}]*mix-blend-mode: screen", RegexOptions.Singleline),
        "The holo white reflection must live on its own `screen` layer.");
    Require(css.Contains("radial-gradient", StringComparison.Ordinal)
        && css.Contains("aeterni-flash-card__glint-sweep", StringComparison.Ordinal),
        "The finish must combine a pointer-phased highlight with a separate moving glint.");
    Require(css.Contains("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal), "FlashCard must honour the reduced motion preference.");

    var script = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/FlashCard/FlashCard.razor.js"));
    Require(script.Contains("export function dispose", StringComparison.Ordinal) && script.Contains("removeEventListener", StringComparison.Ordinal),
        "The FlashCard module must release its pointer listeners.");
    Require(script.Contains("--aeterni-flash-card-sheen-x", StringComparison.Ordinal),
        "The module must drive the sheen position together with the rotation.");

    static bool Throws(System.Reflection.MethodInfo method, object target)
    {
        try
        {
            method.Invoke(target, null);
            return false;
        }
        catch (System.Reflection.TargetInvocationException)
        {
            return true;
        }
    }
}

async Task CheckFlashCardGroupAsync()
{
    RenderFragment cards = builder =>
    {
        builder.OpenComponent<FlashCard>(0);
        builder.AddAttribute(1, nameof(FlashCard.Title), "First");
        builder.CloseComponent();
        builder.OpenComponent<FlashCard>(2);
        builder.AddAttribute(3, nameof(FlashCard.Title), "Second");
        builder.CloseComponent();
    };

    var html = await RenderAsync<FlashCardGroup>(new Dictionary<string, object?>
    {
        [nameof(FlashCardGroup.ChildContent)] = cards,
        [nameof(FlashCardGroup.AriaLabel)] = "Deck",
        [nameof(FlashCardGroup.Expanded)] = true,
        [nameof(FlashCardGroup.CardWidth)] = 240d,
        [nameof(FlashCardGroup.ExpandedSpacing)] = 80d,
        [nameof(FlashCardGroup.CollapsedOffset)] = 6d,
        [nameof(FlashCardGroup.Id)] = "contract-deck"
    });

    Require(html.Contains("id=\"contract-deck\"", StringComparison.Ordinal)
        && html.Contains("role=\"group\"", StringComparison.Ordinal)
        && html.Contains("aria-label=\"Deck\"", StringComparison.Ordinal)
        && html.Contains("is-expanded", StringComparison.Ordinal)
        && html.Contains("--aeterni-flash-card-group-card-width: 240px", StringComparison.Ordinal),
        "FlashCardGroup must preserve its root semantics, expanded state and layout properties.");
    Require(html.Contains("aeterni-flash-card-group__cards", StringComparison.Ordinal)
        && html.Contains("First", StringComparison.Ordinal)
        && html.Contains("Second", StringComparison.Ordinal),
        "FlashCardGroup must render its child card collection.");

    var invalid = new FlashCardGroup();
    typeof(FlashCardGroup).GetProperty(nameof(FlashCardGroup.CardWidth))!.SetValue(invalid, 0d);
    var onParametersSet = typeof(FlashCardGroup).GetMethod("OnParametersSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
    try
    {
        onParametersSet.Invoke(invalid, null);
        Require(false, "FlashCardGroup must reject a non-positive card width.");
    }
    catch (System.Reflection.TargetInvocationException)
    {
        // Expected validation failure.
    }
}

// Regression guards for the tenth review round's batch 23 (P1). One assertion
// per REV item so the pre-fix shape cannot come back unnoticed.
async Task CheckReviewBatch23Async()
{
    // REV-133: a standalone Radio (no RadioGroup) hard-wired IsChecked to false,
    // so an initial value, a programmatic assignment and a form reset were all
    // invisible. Grouped radios keep comparing against the group value.
    var standaloneOn = await RenderAsync<AeterniUI.Components.Radio.Radio<bool>>(new Dictionary<string, object?>
    {
        ["Value"] = true
    });
    Require(Regex.IsMatch(standaloneOn, "<input[^>]*type=\"radio\"[^>]*checked"),
        "A standalone Radio must render checked when its bound value is set.");
    Require(Regex.IsMatch(standaloneOn, "<label[^>]*is-checked[^>]*>"),
        "The standalone Radio root must carry is-checked so the visual circle follows the input.");

    var standaloneOff = await RenderAsync<AeterniUI.Components.Radio.Radio<bool>>(new Dictionary<string, object?>
    {
        ["Value"] = false
    });
    Require(!standaloneOff.Contains("checked"),
        "A standalone Radio must stay unchecked when its bound value is false.");

    var standaloneEmpty = await RenderAsync<AeterniUI.Components.Radio.Radio<string>>(new Dictionary<string, object?>
    {
        ["Value"] = string.Empty
    });
    Require(!standaloneEmpty.Contains("checked"),
        "A standalone Radio must stay unchecked for an empty string value.");

    var standaloneText = await RenderAsync<AeterniUI.Components.Radio.Radio<string>>(new Dictionary<string, object?>
    {
        ["Value"] = "independent"
    });
    Require(standaloneText.Contains("checked"),
        "A standalone Radio must render checked for a non-empty value.");

    // The grouped shape must not regress: only the option matching the group
    // value renders checked.
    var grouped = await RenderAsync<AeterniUI.Components.Radio.RadioGroup<string>>(new Dictionary<string, object?>
    {
        ["Value"] = "b",
        ["ChildContent"] = (RenderFragment)(builder =>
        {
            builder.OpenComponent<AeterniUI.Components.Radio.Radio<string>>(0);
            builder.AddAttribute(1, "Value", "a");
            builder.CloseComponent();
            builder.OpenComponent<AeterniUI.Components.Radio.Radio<string>>(2);
            builder.AddAttribute(3, "Value", "b");
            builder.CloseComponent();
        })
    });
    var groupedInputs = Regex.Matches(grouped, "<input[^>]*type=\"radio\"[^>]*>");
    Require(groupedInputs.Count == 2 && !groupedInputs[0].Value.Contains("checked") && groupedInputs[1].Value.Contains("checked"),
        "A grouped Radio must still derive its checked state from the RadioGroup value.");

    // REV-136: the MultiSelect trigger had no id, so the FormField label's `for`
    // resolved to nothing and clicking the label could not focus the control.
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        MultiSelectContractHost host = null!;
        var root = await renderer.RenderComponentAsync<MultiSelectContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<MultiSelectContractHost>)(x => host = x)
        }));

        var html = root.ToHtmlString();
        var trigger = Regex.Match(html, "<div[^>]*role=\"combobox\"[^>]*>").Value;
        var triggerId = Regex.Match(trigger, "id=\"([^\"]+)\"").Groups[1].Value;
        Require(triggerId.EndsWith("-input", StringComparison.Ordinal),
            "The MultiSelect trigger must adopt the FormField input id.");
        Require(html.Contains($"for=\"{triggerId}\"", StringComparison.Ordinal),
            "The FormField label must target the MultiSelect trigger so clicking it focuses the control.");
        Require(host.Field.ElementId != triggerId,
            "The wrapper keeps the component id; only the trigger adopts the field id.");
    });

    // REV-137: Descriptions rendered a hard-coded English `aria-label="Empty value"`,
    // the only literal aria-label in the library. It must come from the text table
    // and follow an override, otherwise a locally-hosted app cannot localise it.
    var descriptionsDefault = await RenderAsync<Descriptions>(new Dictionary<string, object?>
    {
        ["Items"] = new[] { new DescriptionItem("Owner") }
    });
    Require(descriptionsDefault.Contains("aria-label=\"Empty value\"", StringComparison.Ordinal),
        "Descriptions must take the empty-value accessible name from the text table default.");

    var overrideServices = new ServiceCollection();
    overrideServices.AddLogging();
    overrideServices.AddSingleton<IJSRuntime, NoopJsRuntime>();
    overrideServices.AddAeterniUI(options => options.Text.DescriptionsEmptyValueLabel = "Kein Wert");
    await using var overrideProvider = overrideServices.BuildServiceProvider();
    await using var overrideRenderer = new HtmlRenderer(overrideProvider, overrideProvider.GetRequiredService<ILoggerFactory>());
    var descriptionsOverride = await overrideRenderer.Dispatcher.InvokeAsync(async () =>
    {
        var output = await overrideRenderer.RenderComponentAsync<Descriptions>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Items"] = new[] { new DescriptionItem("Owner") }
        }));
        return output.ToHtmlString();
    });
    Require(descriptionsOverride.Contains("aria-label=\"Kein Wert\"", StringComparison.Ordinal),
        "Descriptions empty-value label must follow AeterniUIOptions.Text instead of a hard-coded string.");

    // REV-138: IconButton painted its loading state with the disabled ink, so the
    // busy glyph fell to ~2.5:1 grey. Loading must be excluded from that paint and
    // keep a variant ink role of its own.
    var iconButtonCss = await File.ReadAllTextAsync(Path.Combine(FindRepositoryRoot(), "src", "AeterniUI", "Components", "IconButton", "IconButton.razor.css"));
    Require(iconButtonCss.Contains(":disabled:not(.is-loading)"),
        "IconButton must exclude its loading state from the disabled paint.");
    Require(iconButtonCss.Contains("--aeterni-icon-button-loading-foreground"),
        "IconButton loading must carry a variant ink role instead of the disabled colour.");
    Require(iconButtonCss.Contains("color: var(--aeterni-icon-button-loading-foreground)"),
        "The IconButton loading glyph must consume that ink role.");

    // REV-134/135: stepping an unbounded field used to wrap (int) or throw
    // (decimal), and clamping before rounding could commit a value above Max.
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        UnboundedInputNumberContractHost host = null!;
        var root = await renderer.RenderComponentAsync<UnboundedInputNumberContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<UnboundedInputNumberContractHost>)(x => host = x)
        }));

        async Task Press(object field, string key)
        {
            var method = field.GetType().GetMethod("HandleKeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            await (Task)method.Invoke(field, [new KeyboardEventArgs { Key = key }])!;
        }

        await Press(host.IntField, "ArrowUp");
        Require(host.IntValue == int.MaxValue,
            $"Stepping an unbounded int field at its maximum must saturate, got {host.IntValue}.");
        Require(host.Changes.Count == 0,
            "A saturated step reports no change instead of wrapping to the opposite extreme.");

        await Press(host.DecimalField, "ArrowUp");
        Require(host.DecimalValue == decimal.MaxValue,
            $"Stepping an unbounded decimal field at its maximum must saturate, got {host.DecimalValue}.");
        await Press(host.DecimalField, "PageUp");
        Require(host.DecimalValue == decimal.MaxValue,
            "A coarse step must saturate as well instead of overflowing in the multiplier.");
        Require(host.Changes.Count == 0, "Saturated decimal steps report no change.");

        await Press(host.GridField, "ArrowUp");
        Require(host.GridValue == 0.35m,
            $"Stepping into an off-grid Max must land on that Max, got {host.GridValue}.");

        // With the value on the bound, the increment affordance has to agree: the
        // step basis comparison used to be made against a value that had already
        // been rounded back above Max.
        host.Changes.Clear();
        await Press(host.GridField, "ArrowUp");
        Require(host.Changes.Count == 0 && host.GridValue == 0.35m,
            "A field sitting on its off-grid Max must report no further increase.");
    });
}

// Regression guards for the tenth review round's batch 24 (P1).
async Task CheckReviewBatch24Async()
{
    // REV-142: AlertOptions.CloseText was a dead parameter — the only place that
    // rendered it was an Alert dialog branch nothing created, while the alert
    // notification read the text table directly. It now names the close button of
    // the alert notification and falls back to the table when left empty.
    async Task<string> RenderAlertsAsync(Action<AeterniUIOptions>? configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        if (configure is null)
        {
            services.AddAeterniUI();
        }
        else
        {
            services.AddAeterniUI(configure);
        }

        await using var currentProvider = services.BuildServiceProvider();
        await using var currentRenderer = new HtmlRenderer(currentProvider, currentProvider.GetRequiredService<ILoggerFactory>());
        var dialogs = currentProvider.GetRequiredService<IDialogService>();
        // TimeSpan.Zero keeps both alerts in the stack instead of racing a timeout.
        dialogs.DefaultAlertDuration = TimeSpan.Zero;
        _ = dialogs.AlertAsync("Saved", new AlertOptions { CloseText = "Dismiss notice" });
        _ = dialogs.AlertAsync("Queued");

        return await currentRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await currentRenderer.RenderComponentAsync<DialogProvider>(ParameterView.Empty);
            return output.ToHtmlString();
        });
    }

    var alertHtml = await RenderAlertsAsync(null);
    Require(alertHtml.Contains("aria-label=\"Dismiss notice\"", StringComparison.Ordinal),
        "AlertOptions.CloseText must name the alert notification's close button.");
    Require(alertHtml.Contains("aria-label=\"Close alert\"", StringComparison.Ordinal),
        "An alert without CloseText must fall back to the text table's AlertCloseLabel.");

    var localizedAlerts = await RenderAlertsAsync(options => options.Text.AlertCloseLabel = "关闭提示");
    // Attribute values are HTML-encoded by the renderer, so the expectation has to
    // be encoded the same way (the same trap REV-143 fixed for the Timeline timestamps).
    var localizedLabel = System.Text.Encodings.Web.HtmlEncoder.Default.Encode("关闭提示");
    var localizedLabels = string.Join(" | ", System.Text.RegularExpressions.Regex.Matches(localizedAlerts, "aria-label=\"([^\"]+)\"")
        .Select(m => m.Groups[1].Value));
    Require(localizedAlerts.Contains($"aria-label=\"{localizedLabel}\"", StringComparison.Ordinal)
        && localizedAlerts.Contains("aria-label=\"Dismiss notice\"", StringComparison.Ordinal),
        $"The alert close label must read the text table, and an explicit CloseText must still win (found: {localizedLabels}).");
}

// Regression guards for the tenth review round's batch 26 (P2). Hygiene items:
// token discipline, dead classes, parameter validation and file responsibility.
async Task CheckReviewBatch26Async()
{
    var root = FindRepositoryRoot();
    var components = Path.Combine(root, "src", "AeterniUI", "Components");
    var componentCss = Directory.GetFiles(components, "*.razor.css", SearchOption.AllDirectories);

    // REV-145: component styles must not consume the host aliases; those belong to
    // the host so overriding them there cannot move a component's inner spacing.
    var aliasOffenders = componentCss
        .Where(file => Regex.IsMatch(File.ReadAllText(file), @"var\(--aeterni-(padding|gap|margin)-"))
        .Select(file => Path.GetFileName(file))
        .ToArray();
    Require(aliasOffenders.Length == 0,
        $"Component styles must use --aeterni-spacing-* instead of the host aliases, found: {string.Join(", ", aliasOffenders)}.");

    // REV-149: the invalid class used to be emitted with no rule at all.
    var ratingCss = await File.ReadAllTextAsync(Path.Combine(components, "Rating", "Rating.razor.css"));
    Require(Regex.IsMatch(ratingCss, @"\.aeterni-rating\.is-invalid[^\{]*\{[^}]*--aeterni-rating-star", RegexOptions.Singleline),
        "Rating must show its invalid state on the control, not only through the field message.");

    // REV-150: a scoped `> svg` rule can never match a child component's markup, and
    // the default variant must not emit a modifier class.
    var tagCss = await File.ReadAllTextAsync(Path.Combine(components, "Tag", "Tag.razor.css"));
    Require(!tagCss.Contains(".aeterni-tag__icon > svg", StringComparison.Ordinal)
        && tagCss.Contains("--aeterni-icon-render-size: 100%", StringComparison.Ordinal),
        "Tag must size the child Icon through the shared token instead of a scoped svg rule.");
    var defaultTag = await RenderAsync<AeterniUI.Components.Tag.Tag>(new Dictionary<string, object?>
    {
        ["ChildContent"] = (RenderFragment)(builder => builder.AddContent(0, "tag"))
    });
    var softTag = await RenderAsync<AeterniUI.Components.Tag.Tag>(new Dictionary<string, object?>
    {
        ["Variant"] = TagVariant.Soft,
        ["ChildContent"] = (RenderFragment)(builder => builder.AddContent(0, "tag"))
    });
    var outlineTag = await RenderAsync<AeterniUI.Components.Tag.Tag>(new Dictionary<string, object?>
    {
        ["Variant"] = TagVariant.Outline,
        ["ChildContent"] = (RenderFragment)(builder => builder.AddContent(0, "tag"))
    });
    Require(!defaultTag.Contains("aeterni-tag--default", StringComparison.Ordinal)
        && softTag.Contains("aeterni-tag--soft", StringComparison.Ordinal)
        && outlineTag.Contains("aeterni-tag--outline", StringComparison.Ordinal),
        "Only the default Tag variant may stay silent; Soft and Outline keep their modifiers.");

    // REV-151: one role token per effect, no side doors.
    var timelineCss = await File.ReadAllTextAsync(Path.Combine(components, "Timeline", "Timeline.razor.css"));
    Require(timelineCss.Contains("var(--aeterni-scrollbar-thumb)", StringComparison.Ordinal)
        && !timelineCss.Contains("var(--aeterni-border-strong)", StringComparison.Ordinal),
        "Timeline must use the shared scrollbar role tokens.");
    var segmentedCss = await File.ReadAllTextAsync(Path.Combine(components, "Segmented", "Segmented.razor.css"));
    Require(!segmentedCss.Contains("var(--aeterni-color-danger-default)", StringComparison.Ordinal)
        && segmentedCss.Contains("var(--aeterni-state-border-invalid)", StringComparison.Ordinal),
        "Segmented must express invalid through the state role token so a host override applies.");
    var placeholderOffenders = componentCss
        .Where(file => File.ReadAllText(file).Contains("var(--aeterni-state-color-placeholder)", StringComparison.Ordinal))
        .Select(file => Path.GetFileName(file))
        .ToArray();
    Require(placeholderOffenders.Length == 0,
        $"The date and time fields must agree on the control placeholder role, found: {string.Join(", ", placeholderOffenders)}.");

    // REV-152: classes with no consumer and a keyframe with no animation.
    foreach (var (component, dead) in new[]
    {
        ("MenuButton", "is-open"),
        ("SplitButton", "is-disabled"),
        ("ButtonGroup", "is-disabled"),
        ("IconButton", "is-disabled"),
    })
    {
        var code = await File.ReadAllTextAsync(Path.Combine(components, component, $"{component}.razor.cs"));
        var markup = File.Exists(Path.Combine(components, component, $"{component}.razor"))
            ? await File.ReadAllTextAsync(Path.Combine(components, component, $"{component}.razor"))
            : string.Empty;
        Require(!code.Contains($"\"{dead}\"", StringComparison.Ordinal) && !markup.Contains($"\"{dead}\"", StringComparison.Ordinal),
            $"{component} must not emit the dead class '{dead}'.");
    }

    var horizontalGroup = await RenderAsync<AeterniUI.Components.Radio.RadioGroup<string>>(new Dictionary<string, object?>
    {
        ["ChildContent"] = (RenderFragment)(_ => { })
    });
    var verticalGroup = await RenderAsync<AeterniUI.Components.Radio.RadioGroup<string>>(new Dictionary<string, object?>
    {
        ["Orientation"] = Orientation.Vertical,
        ["ChildContent"] = (RenderFragment)(_ => { })
    });
    Require(!horizontalGroup.Contains("aeterni-radio-group--horizontal", StringComparison.Ordinal)
        && horizontalGroup.Contains("aria-orientation=\"horizontal\"", StringComparison.Ordinal),
        "The default RadioGroup orientation must keep its ARIA value without emitting a modifier class.");
    Require(verticalGroup.Contains("aeterni-radio-group--vertical", StringComparison.Ordinal)
        && verticalGroup.Contains("aria-orientation=\"vertical\"", StringComparison.Ordinal),
        "The vertical RadioGroup keeps both its modifier class and its ARIA value.");
    var dialogCss = await File.ReadAllTextAsync(Path.Combine(components, "Dialog", "DialogProvider.razor.css"));
    Require(!dialogCss.Contains("aeterni-dialog-slide-in", StringComparison.Ordinal),
        "An animation with no reference must be deleted.");
    var docs = await File.ReadAllTextAsync(Path.Combine(root, "docs", "delivered-features.zh-CN.md"));
    Require(docs.Contains("aeterni-{component}__host", StringComparison.Ordinal),
        "The popup host class must be documented as a host styling hook (or removed).");

    // REV-156 ①: an explicitly null list has to name the parameter instead of
    // surfacing as an internal NRE somewhere inside the component.
    const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    foreach (var (name, instance) in new (string, object)[]
    {
        ("Menu", new Menu { Items = null! }),
        ("Segmented", new Segmented<string> { Items = null! }),
    })
    {
        var threw = false;
        try
        {
            instance.GetType().GetMethod("OnParametersSet", flags)!.Invoke(instance, null);
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is ArgumentNullException)
        {
            threw = true;
        }

        Require(threw, $"{name} must reject an explicitly null Items collection with ArgumentNullException.");
    }

    // REV-156 ②: the confirmation buttons read the text table when the host leaves
    // the labels empty, and an explicit label still wins.
    async Task<string> RenderConfirmAsync(Action<AeterniUIOptions>? configure, ConfirmOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        if (configure is null)
        {
            services.AddAeterniUI();
        }
        else
        {
            services.AddAeterniUI(configure);
        }

        await using var currentProvider = services.BuildServiceProvider();
        await using var currentRenderer = new HtmlRenderer(currentProvider, currentProvider.GetRequiredService<ILoggerFactory>());
        var dialogs = currentProvider.GetRequiredService<IDialogService>();
        _ = dialogs.ConfirmAsync("Publish the package?", options);

        return await currentRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await currentRenderer.RenderComponentAsync<DialogProvider>(ParameterView.Empty);
            return output.ToHtmlString();
        });
    }

    var defaultConfirm = await RenderConfirmAsync(null, new ConfirmOptions());
    Require(defaultConfirm.Contains(">Confirm<", StringComparison.Ordinal)
        && defaultConfirm.Contains(">Cancel<", StringComparison.Ordinal),
        "An unset confirmation dialog must fall back to the text table labels.");

    var localisedConfirm = await RenderConfirmAsync(options => options.Text.ConfirmAcceptLabel = "发布", new ConfirmOptions());
    // Text content is HTML-encoded by the renderer, so the expectation has to be too.
    var encodedLabel = System.Text.Encodings.Web.HtmlEncoder.Default.Encode("发布");
    Require(localisedConfirm.Contains($">{encodedLabel}<", StringComparison.Ordinal),
        "The confirmation labels must come from the text table so a localised host gets them.");

    var explicitConfirm = await RenderConfirmAsync(
        options => options.Text.ConfirmAcceptLabel = "发布",
        new ConfirmOptions { ConfirmText = "Ship it" });
    Require(explicitConfirm.Contains(">Ship it<", StringComparison.Ordinal)
        && !explicitConfirm.Contains(">发布<", StringComparison.Ordinal),
        "An explicitly set confirmation label must win over the text table.");

    // REV-156 ② + REV-157 ①: the visible confirmation labels fall back to the text
    // table, and IconButton's render logic lives in its code-behind.
    var iconButtonMarkup = await File.ReadAllTextAsync(Path.Combine(components, "IconButton", "IconButton.razor"));
    var iconButtonCode = await File.ReadAllTextAsync(Path.Combine(components, "IconButton", "IconButton.razor.cs"));
    Require(!iconButtonMarkup.Contains("@code", StringComparison.Ordinal)
        && iconButtonCode.Contains("protected override ClassBuilder BuildClass()", StringComparison.Ordinal)
        && iconButtonCode.Contains("private async Task HandleClickAsync", StringComparison.Ordinal),
        "IconButton render logic belongs in the code-behind like its siblings.");

    var designDocs = await File.ReadAllTextAsync(Path.Combine(root, "docs", "engineering-reference.zh-CN.md"));
    Require(designDocs.Contains("四个角色名", StringComparison.Ordinal) && !designDocs.Contains("五个角色名", StringComparison.Ordinal),
        "The glass role list must say four roles, matching the implementation.");
}

// Regression guards for the tenth review round's batch 25 (P2). Each assertion
// locks one REV item in place.
async Task CheckReviewBatch25Async()
{
    var root = FindRepositoryRoot();

    // REV-146 ①: the pointer needs the same feedback the keyboard cursor already
    // had, so a selected option must step its fill one level deeper on hover.
    var comboCss = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/ComboBox/ComboBox.razor.css"));
    Require(Regex.IsMatch(comboCss,
            @"\.aeterni-combobox__option\.is-selected:hover:not\(\.is-disabled\) \{[^}]*background: var\(--aeterni-state-background-active\)",
            RegexOptions.Singleline),
        "A hovered selected ComboBox option must step its fill to the active stop.");

    // REV-146 ②: the connected groups own the visible border (the inner Input has
    // its own border flattened), so they need the hover border standalone Input has.
    foreach (var (component, group, wrapper) in new[]
    {
        ("Search", ".aeterni-search", ".aeterni-search__input-wrap"),
        ("Autocomplete", ".aeterni-autocomplete", ".aeterni-autocomplete__input-wrap"),
        ("InputNumber", ".aeterni-input-number", ".aeterni-input-number__field"),
    })
    {
        var css = await File.ReadAllTextAsync(Path.Combine(root, $"src/AeterniUI/Components/{component}/{component}.razor.css"));
        Require(css.Contains($"{group}:not(.is-invalid):not(.is-disabled):not(.is-readonly) {wrapper}:hover:not(:focus-within)", StringComparison.Ordinal),
            $"{component} must give its connected field a hover border that excludes focus, invalid, disabled and read-only.");
        Require(Regex.IsMatch(css,
                $@"{Regex.Escape(group)}:not\(\.is-invalid\):not\(\.is-disabled\):not\(\.is-readonly\) {Regex.Escape(wrapper)}:hover:not\(:focus-within\) \{{[^}}]*--aeterni-control-border-hover",
                RegexOptions.Singleline),
            $"{component} must use the shared hover border colour for that state.");
    }

    // REV-147: the roving modules of Toolbar/ToggleGroup/Menu must receive the node
    // they render, not RootElement — a host-supplied Element parameter replaces
    // RootElement, which used to fail the module's ownership check silently.
    foreach (var component in new[] { "Toolbar", "ToggleGroup", "Menu" })
    {
        var code = await File.ReadAllTextAsync(Path.Combine(root, $"src/AeterniUI/Components/{component}/{component}.razor.cs"));
        Require(!code.Contains("InstanceId, RootElement", StringComparison.Ordinal),
            $"{component} must not hand RootElement to its JS module.");
        Require(code.Contains("RenderedRootElement", StringComparison.Ordinal),
            $"{component} must keep an internal reference to the element it renders.");
        var markup = await File.ReadAllTextAsync(Path.Combine(root, $"src/AeterniUI/Components/{component}/{component}.razor"));
        Require(markup.Contains("@ref=\"RenderedRootElement\"", StringComparison.Ordinal),
            $"{component} must bind that internal reference on its rendered root.");
    }

    // REV-153: a JS-side runtime error has to degrade like a disconnected circuit;
    // it used to escape as an unhandled render exception.
    var faultRuntime = new JsFaultRuntime();
    await using (var faultManager = new JsModuleManager(faultRuntime))
    {
        faultManager.ScanComponent(typeof(Slider<int>));
        var escaped = false;
        try
        {
            await faultManager.InvokeModuleVoidAsync("slider", "sync", "instance", default(ElementReference), true);
        }
        catch (JSException)
        {
            escaped = true;
        }

        Require(!escaped, "A throwing JS module must not surface as an unhandled render exception.");

        // Reported instead of thrown: a gate has to list this failure rather than
        // abort the whole run with the exception it is asserting about.
        bool? queried = null;
        try
        {
            queried = await faultManager.InvokeModuleAsync<bool>("slider", "isDragging");
        }
        catch (JSException)
        {
        }

        Require(queried == false, "A throwing JS module must resolve the querying path to its default value.");
    }

    // REV-154: the visible month may only follow a real value change. OnParametersSet
    // runs on every parent render, which used to undo a month the user paged to.
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        async Task CheckMonthHoldsAsync<TComponent>(string valueKey, object initialValue, object changedValue, DateOnly paged)
            where TComponent : IComponent
        {
            ParameterContractHost<TComponent> host = null!;
            await renderer.RenderComponentAsync<ParameterContractHost<TComponent>>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Ready"] = (Action<ParameterContractHost<TComponent>>)(x => host = x)
            }));
            host.Values = new Dictionary<string, object?> { [valueKey] = initialValue };
            host.Update();

            var month = typeof(TComponent).GetField("_displayMonth", flags)!;
            var name = typeof(TComponent).Name;
            Require((DateOnly)month.GetValue(host.Inner)! == new DateOnly(2026, 1, 1),
                $"{name} must open on the month of its value.");

            month.SetValue(host.Inner, paged);

            // Re-supplying the same parameters is exactly what a parent render does
            // (and what used to snap the month back).
            await ((IComponent)host.Inner).SetParametersAsync(ParameterView.FromDictionary(host.Values));
            var afterRender = (DateOnly)month.GetValue(host.Inner)!;
            Require(afterRender == paged,
                $"{name} must keep a paged month when its parameters are re-applied unchanged (REV-154); got {afterRender:yyyy-MM-dd} instead of {paged:yyyy-MM-dd}.");

            host.Values[valueKey] = changedValue;
            await ((IComponent)host.Inner).SetParametersAsync(ParameterView.FromDictionary(host.Values));
            Require((DateOnly)month.GetValue(host.Inner)! == new DateOnly(2026, 3, 1),
                $"{name} must follow the month of a changed value.");
        }

        var paged = new DateOnly(2026, 6, 1);
        await CheckMonthHoldsAsync<DatePicker>("Value", new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 10), paged);
        await CheckMonthHoldsAsync<DateRangePicker>("StartDate", new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 10), paged);
        await CheckMonthHoldsAsync<DateTimePicker>("Value", new DateTime(2026, 1, 15, 9, 30, 0), new DateTime(2026, 3, 10, 9, 30, 0), paged);
    });

    // REV-148: retiring a closing entry waits for the measured exit animation instead
    // of a fixed 180/220ms delay that cut the 300ms animation off. The provider
    // reports the measured end; the bounded fallback keeps a provider-less host
    // (or a runtime without modules) from hanging.
    var dialogCode = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Services/Impl/DialogService.cs"));
    Require(!dialogCode.Contains("Task.Delay(TimeSpan.FromMilliseconds(180)", StringComparison.Ordinal)
        && !dialogCode.Contains("Task.Delay(TimeSpan.FromMilliseconds(220)", StringComparison.Ordinal),
        "The service must not retire a closing entry after a fixed delay.");
    Require(dialogCode.Contains("GetClosingIds", StringComparison.Ordinal)
        && dialogCode.Contains("SignalExit", StringComparison.Ordinal)
        && dialogCode.Contains("ExitSignalFallback", StringComparison.Ordinal),
        "The service must expose the measured exit signal and keep a bounded fallback.");
    var dialogModule = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/Dialog/DialogProvider.razor.js"));
    Require(dialogModule.Contains("export async function waitForExitSignals", StringComparison.Ordinal)
        && dialogModule.Contains("waitForExit(", StringComparison.Ordinal),
        "The dialog module must measure the real exit animation.");
    var dialogProvider = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/Dialog/DialogProvider.razor.cs"));
    Require(dialogProvider.Contains("\"waitForExitSignals\"", StringComparison.Ordinal)
        && dialogProvider.Contains("DialogService.SignalExit(id)", StringComparison.Ordinal),
        "The provider must drive the exit signal back into the service.");

    // HtmlRenderer is a static renderer: it never runs OnAfterRenderAsync, so the
    // provider's own hook-up is covered by the assertions above plus the browser
    // check, and the signal contract is exercised against the service directly.
    async Task<TimeSpan> MeasureToastCloseAsync(bool signal)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddAeterniUI();
        await using var toastProvider = services.BuildServiceProvider();
        var dialogs = (DialogService)toastProvider.GetRequiredService<IDialogService>();
        dialogs.DefaultToastDuration = TimeSpan.Zero;

        var toast = dialogs.ShowToast("Saved");
        var close = toast.CloseAsync();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (signal)
        {
            // A closing entry is only visible while the close is pending, so the
            // signal has to arrive from another task, like the provider's does.
            await Task.Delay(30);
            var signalMethod = typeof(DialogService).GetMethod("SignalExit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            signalMethod.Invoke(dialogs, [toast.Id]);
        }

        await close;
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    var signalled = await MeasureToastCloseAsync(signal: true);
    Require(signalled < TimeSpan.FromMilliseconds(700),
        $"The measured exit signal must retire the entry promptly, took {signalled.TotalMilliseconds:0}ms.");
    var fallback = await MeasureToastCloseAsync(signal: false);
    Require(fallback >= TimeSpan.FromMilliseconds(900),
        $"Without a signal the bounded fallback must apply, took {fallback.TotalMilliseconds:0}ms.");

    // REV-155: a field must join the host's description id instead of replacing it,
    // matching ComboBox and Autocomplete.
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        MultiSelectContractHost host = null!;
        var output = await renderer.RenderComponentAsync<MultiSelectContractHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<MultiSelectContractHost>)(x => host = x)
        }));
        host.AriaDescribedBy = "host-hint";
        host.Update();

        var html = output.ToHtmlString();
        var describedBy = Regex.Match(html, "<div[^>]*role=\"combobox\"[^>]*aria-describedby=\"([^\"]+)\"");
        var ids = describedBy.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Require(ids.Contains("host-hint") && ids.Length >= 2,
            $"MultiSelect must merge the host description id with the field's own (got '{describedBy.Groups[1].Value}').");
    });
}

// Regression guards for the ninth review round's P0 batch. Each assertion locks a
// fix in place so the pre-fix shape cannot come back unnoticed.
async Task CheckArchitectureP0Async()
{
    var root = FindRepositoryRoot();

    // REV-97: the Radio root is a <label>, which cannot carry the native disabled
    // attribute. Disabling must surface through the input plus one aria-disabled.
    var radio = await RenderAsync<AeterniUI.Components.Radio.Radio<string>>(new Dictionary<string, object?>
    {
        ["Value"] = "a",
        ["Disabled"] = true
    });
    var radioRoot = Regex.Match(radio, "<label[^>]*>").Value;
    // Match the attribute itself: `aria-disabled="true"` also contains the
    // substring "disabled", so a plain Contains would never pass.
    Require(!Regex.IsMatch(radioRoot, @"\sdisabled="),
        "Radio label root must not emit native disabled (invalid on <label>).");
    Require(radioRoot.Contains("aria-disabled=\"true\""), "Radio label root must still report aria-disabled.");
    Require(Regex.Match(radio, "<input[^>]*type=\"radio\"[^>]*>").Value.Contains("disabled"),
        "Radio input must keep the native disabled state.");

    // REV-99: an undefined ButtonType used to render verbatim as type="99", and
    // HTML resolves an invalid type to `submit`, silently submitting the form.
    try
    {
        await RenderAsync<AeterniUI.Components.Button.Button>(new Dictionary<string, object?>
        {
            [nameof(AeterniUI.Components.Button.Button.Type)] = (ButtonType)99
        });
        Require(false, "Button must reject an unknown ButtonType instead of rendering type=\"99\".");
    }
    catch (ArgumentOutOfRangeException) { }

    var submitButton = await RenderAsync<AeterniUI.Components.Button.Button>(new Dictionary<string, object?>
    {
        [nameof(AeterniUI.Components.Button.Button.Type)] = ButtonType.Submit
    });
    Require(submitButton.Contains("type=\"submit\""), "Button must render ButtonType.Submit as the native submit type.");

    // REV-102: DateCalendar inherits the base DOM contract, so the inherited
    // parameters must reach the rendered root instead of being dead.
    var calendar = await RenderAsync<DateCalendar>(new Dictionary<string, object?>
    {
        [nameof(DateCalendar.DisplayMonth)] = new DateOnly(2026, 9, 1),
        ["Class"] = "consumer-calendar",
        ["Id"] = "consumer-calendar-id"
    });
    Require(calendar.Contains("consumer-calendar", StringComparison.Ordinal)
        && calendar.Contains("id=\"consumer-calendar-id\"", StringComparison.Ordinal),
        "DateCalendar must honour consumer Class and Id through the base DOM contract.");

    // REV-98: the provider root must not create a stacking context, or the notice
    // region is clamped below popovers (1060) and tooltips (1070).
    var dialogCss = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/Dialog/DialogProvider.razor.css"));
    var providerRoot = Regex.Match(dialogCss, @"\.aeterni-dialog-provider\s*\{[^}]*\}").Value;
    Require(!providerRoot.Contains("position", StringComparison.Ordinal)
        && !providerRoot.Contains("z-index", StringComparison.Ordinal)
        && !providerRoot.Contains("isolation", StringComparison.Ordinal),
        "The dialog provider root must not create a stacking context.");

    // REV-101: the transition rule drives the class ThemeProvider toggles, so it
    // must ship with the library rather than only in the sample host.
    var tokens = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/wwwroot/css/aeterni_ui.css"));
    Require(tokens.Contains("html.aeterni-theme-transitioning", StringComparison.Ordinal),
        "The theme transition rule must ship with the library.");
    var sampleCss = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI.Sample/wwwroot/css/app.css"));
    Require(!sampleCss.Contains("html.aeterni-theme-transitioning", StringComparison.Ordinal),
        "Hosts must not restate the library theme transition rule.");

    // REV-103: the combobox modules need the real trigger/input element. Passing
    // RootElement let a consumer-supplied Element parameter silently disable
    // keyboard suppression and active-option scrolling.
    foreach (var component in new[] { "MultiSelect", "Autocomplete" })
    {
        var code = await File.ReadAllTextAsync(Path.Combine(root, $"src/AeterniUI/Components/{component}/{component}.razor.cs"));
        Require(!code.Contains("\"sync\", InstanceId, RootElement", StringComparison.Ordinal),
            $"{component} must pass its real trigger/input element to the JS module, not RootElement.");
    }
}

// Regression guards for the ninth review round's P1 JS-lifecycle batch.
async Task CheckJsLifecycleAsync()
{
    var root = FindRepositoryRoot();

    // REV-105: a failed import must not be cached, or one transient failure
    // silently strips the component of its JS behaviour for the whole circuit.
    var runtime = new FlakyImportJsRuntime();
    await using var manager = new JsModuleManager(runtime);
    manager.ScanComponent(typeof(MultiSelect<string>));
    var first = await manager.GetModuleAsync("multi-select");
    Require(first is null && runtime.ImportAttempts == 1, "A failed module import must resolve to null.");
    await manager.GetModuleAsync("multi-select");
    Require(runtime.ImportAttempts == 2, "A failed module import must not be cached: the next call has to retry.");

    // REV-104: the base class has to pair its dispose with a load that is still in
    // flight, so an instance created by init always has an owner that disposes it.
    var baseClass = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/AeterniComponent.cs"));
    Require(baseClass.Contains("_jsModuleLoadStarted", StringComparison.Ordinal)
        && baseClass.Contains("if (IsDisposed)", StringComparison.Ordinal),
        "AeterniComponent must record that a JS load started and guard the load with IsDisposed.");

    // REV-106: the provider module must address its own instance by key, and may
    // only tear down module-global state once the last provider is gone.
    var dialogJs = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/Components/Dialog/DialogProvider.razor.js"));
    Require(!dialogJs.Contains("[...instances.values()][0]", StringComparison.Ordinal),
        "DialogProvider sync must resolve its own instance by key.");
    Require(dialogJs.Contains("instances.size === 0", StringComparison.Ordinal),
        "DialogProvider may only dispose module-global progress state when no provider is left.");

    // REV-107 / REV-108: a component exposing [JSInvokable] entry points can be
    // called from the browser after release, so it must be disposal-aware. The
    // guard may sit at the entry point or in the funnel it delegates to, which is
    // why this checks the file rather than each method body.
    foreach (var file in Directory.EnumerateFiles(
        Path.Combine(root, "src/AeterniUI/Components"), "*.razor.cs", SearchOption.AllDirectories))
    {
        var source = await File.ReadAllTextAsync(file);
        if (!source.Contains("[JSInvokable]", StringComparison.Ordinal))
        {
            continue;
        }

        Require(source.Contains("IsDisposed", StringComparison.Ordinal) || source.Contains("_disposed", StringComparison.Ordinal),
            $"{Path.GetFileName(file)} exposes [JSInvokable] entry points without any disposal guard.");
    }
}

// Regression guards for the ninth review round's P1 shared-abstraction batch.
async Task CheckSharedAbstractionsAsync()
{
    var root = FindRepositoryRoot();
    var subscriptionFile = Path.Combine(root, "src/AeterniUI/Components/EditContextSubscription.cs");
    var components = Path.Combine(root, "src/AeterniUI/Components");

    foreach (var file in Directory.EnumerateFiles(components, "*.razor.cs", SearchOption.AllDirectories))
    {
        var source = await File.ReadAllTextAsync(file);
        var name = Path.GetFileName(file);

        // REV-109: the EditContext subscription mechanics live in exactly one
        // place. A component that touches OnValidationStateChanged itself is a
        // re-grown copy of the machinery the shared helper exists to own.
        if (!string.Equals(file, subscriptionFile, StringComparison.Ordinal))
        {
            Require(!source.Contains("OnValidationStateChanged", StringComparison.Ordinal),
                $"{name} subscribes to EditContext directly; route it through EditContextSubscription.");
        }

        // REV-111: aria-disabled is emitted only while disabled. An explicit
        // "false" would be a second convention alongside the base class.
        Require(!Regex.IsMatch(source, "\\[\"aria-disabled\"\\]\\s*=\\s*[^;]*\"false\""),
            $"{name} must not render aria-disabled=\"false\".");
    }

    // REV-122: CSS isolation means a component stylesheet cannot be shared, so the
    // button family necessarily keeps two copies of the intent pipeline. That is
    // acceptable; letting them drift is not. Both copies must wire the same slots
    // for every intent they have in common, so adding a slot to one and forgetting
    // the other fails here instead of shipping two different button behaviours.
    static Dictionary<string, string> IntentSlots(string css, string prefix)
    {
        var slots = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(css, $@"\.{Regex.Escape(prefix)}--(\w+)\s*\{{([^}}]*)\}}"))
        {
            // Only the intent blocks wire the shared intent tokens; size and
            // variant modifiers legitimately differ between the two components.
            if (!match.Groups[2].Value.Contains("--aeterni-button-intent-", StringComparison.Ordinal))
            {
                continue;
            }

            // Compare only the declarations that wire the shared intent tokens:
            // the rest of a block is component-specific (Button folds its disabled
            // slots into the same rule, IconButton keeps them separate).
            var declarations = match.Groups[2].Value
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(declaration => declaration.Contains("--aeterni-button-intent-", StringComparison.Ordinal))
                .Select(declaration => Regex.Replace(declaration, @"\s+", " "))
                .OrderBy(declaration => declaration, StringComparer.Ordinal);
            slots[match.Groups[1].Value] = string.Join("|", declarations);
        }

        return slots;
    }

    var buttonIntents = IntentSlots(
        await File.ReadAllTextAsync(Path.Combine(components, "Button/Button.razor.css")), "aeterni-button");
    var iconButtonIntents = IntentSlots(
        await File.ReadAllTextAsync(Path.Combine(components, "IconButton/IconButton.razor.css")), "aeterni-icon-button");
    var sharedIntents = buttonIntents.Keys.Intersect(iconButtonIntents.Keys, StringComparer.Ordinal).ToArray();
    Require(sharedIntents.Length >= 3, "The button family must share at least the neutral, warning and danger intents.");
    foreach (var intent in sharedIntents)
    {
        Require(buttonIntents[intent] == iconButtonIntents[intent],
            $"Button and IconButton must wire identical slots for the '{intent}' intent.");
    }

    // REV-122 / REV-115: Card and Surface render the same surface recipe under
    // different root classes, and CSS isolation means the two stylesheets cannot
    // share a rule body. The duplication that follows is tolerable; drift is not.
    // The variant and elevation bodies must stay identical once the prefix is
    // normalised away. Padding is exempt on purpose — the two components differ in
    // what "unset" means there.
    static Dictionary<string, string> SurfaceRuleBodies(string css, string prefix)
    {
        var bodies = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(css, $@"\.{Regex.Escape(prefix)}(--[a-z-]+)\s*\{{([^}}]*)\}}"))
        {
            bodies[match.Groups[1].Value] = Regex.Replace(match.Groups[2].Value, @"\s+", " ").Trim();
        }

        return bodies;
    }

    var cardBodies = SurfaceRuleBodies(await File.ReadAllTextAsync(Path.Combine(components, "Card/Card.razor.css")), "aeterni-card");
    var surfaceBodies = SurfaceRuleBodies(await File.ReadAllTextAsync(Path.Combine(components, "Surface/Surface.razor.css")), "aeterni-surface");
    // Two families are exempt by design rather than by accident. Padding: the two
    // components differ in what "unset" means. Radius: Card carries the fixed
    // container radius because its header/body/footer structure makes it the
    // larger container, while Surface follows the control geometry and exposes the
    // scale. Everything else must match on both sides — including a modifier that
    // gains only one of them, which the shared-body comparison below cannot see
    // because it only looks at keys that already exist in both.
    static bool SharedSurfaceModifier(string modifier) =>
        !modifier.Contains("padding", StringComparison.Ordinal)
        && !modifier.Contains("radius", StringComparison.Ordinal);

    var cardShared = cardBodies.Keys.Where(SharedSurfaceModifier).OrderBy(m => m, StringComparer.Ordinal).ToArray();
    var surfaceShared = surfaceBodies.Keys.Where(SharedSurfaceModifier).OrderBy(m => m, StringComparer.Ordinal).ToArray();
    Require(cardShared.SequenceEqual(surfaceShared, StringComparer.Ordinal),
        "Card and Surface must declare the same surface modifiers: card-only " +
        $"[{string.Join(", ", cardShared.Except(surfaceShared, StringComparer.Ordinal))}], surface-only " +
        $"[{string.Join(", ", surfaceShared.Except(cardShared, StringComparer.Ordinal))}].");

    var sharedModifiers = cardShared.Intersect(surfaceShared, StringComparer.Ordinal).ToArray();
    // Six: the variant modifiers (subtle, elevated, glass) and the three elevation
    // tiers. Each is one rule body because the variant selectors are grouped.
    Require(sharedModifiers.Length >= 6,
        $"Card and Surface must share at least the variant and elevation modifiers, found {sharedModifiers.Length}.");
    foreach (var modifier in sharedModifiers)
    {
        Require(cardBodies[modifier] == surfaceBodies[modifier],
            $"Card and Surface must render the same surface recipe for '{modifier}'.");
    }

    // REV-127: inheriting the base contract means feeding the root element back.
    // Without the @ref binding, Element and ElementChanged stay dead parameters.
    foreach (var markup in new[] { "Radio/RadioGroup.razor", "Tabs/Tab.razor" })
    {
        var source = await File.ReadAllTextAsync(Path.Combine(components, markup));
        Require(source.Contains("@ref=\"RootElement\"", StringComparison.Ordinal),
            $"{markup} must bind @ref=\"RootElement\" so Element and ElementChanged work.");
    }
}

// Regression guards for the ninth review round's P1 validation and localisation
// batch. Enum validation used to be split: some components threw, others silently
// degraded, and the silent ones had no contract coverage at all.
async Task CheckEnumValidationAsync()
{
    var cases = new (string Name, Func<Task> Render)[]
    {
        ("Button.Variant", () => RenderAsync<AeterniUI.Components.Button.Button>(new Dictionary<string, object?> { ["Variant"] = (ButtonVariant)99 })),
        ("Button.Intent", () => RenderAsync<AeterniUI.Components.Button.Button>(new Dictionary<string, object?> { ["Intent"] = (ButtonIntent)99 })),
        ("Button.Size", () => RenderAsync<AeterniUI.Components.Button.Button>(new Dictionary<string, object?> { ["Size"] = (Size)99 })),
        ("IconButton.AriaLabel", () => RenderAsync<AeterniUI.Components.IconButton.IconButton>(new Dictionary<string, object?> { ["AriaLabel"] = "  " })),
        ("Surface.Variant", () => RenderAsync<AeterniUI.Components.Surface.Surface>(new Dictionary<string, object?> { ["Variant"] = (SurfaceVariant)99 })),
        ("Card.Elevation", () => RenderAsync<AeterniUI.Components.Card.Card>(new Dictionary<string, object?> { ["Elevation"] = (SurfaceElevation)99 })),
        ("Icon.Color", () => RenderAsync<AeterniUI.Components.Icon.Icon>(new Dictionary<string, object?> { ["Color"] = (Color)99 })),
        ("Radio.Size", () => RenderAsync<AeterniUI.Components.Radio.Radio<string>>(new Dictionary<string, object?> { ["Value"] = "a", ["Size"] = (Size)99 })),
        ("ButtonGroup.Orientation", () => RenderAsync<AeterniUI.Components.ButtonGroup.ButtonGroup>(new Dictionary<string, object?> { ["Orientation"] = (Orientation)99 })),
        ("InputNumber.Size", () => RenderAsync<InputNumber<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Size"] = (Size)99 })),
        ("Slider.Size", () => RenderAsync<Slider<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Size"] = (Size)99 })),
        ("Slider.Orientation", () => RenderAsync<Slider<int>>(new Dictionary<string, object?> { ["Value"] = 1, ["Orientation"] = (Orientation)99 })),
        ("Timeline.Orientation", () => RenderAsync<Timeline>(new Dictionary<string, object?> { ["Items"] = Array.Empty<TimelineItem>(), ["Orientation"] = (Orientation)99 })),
        ("ThemeSwitch.Size", () => RenderAsync<AeterniUI.Components.Theme.ThemeSwitch>(new Dictionary<string, object?> { ["Size"] = (Size)99 })),
        ("ToggleGroup.SelectionMode", () => RenderAsync<AeterniUI.Components.ToggleGroup.ToggleGroup>(new Dictionary<string, object?> {
            ["AriaLabel"] = "Actions", ["SelectionMode"] = SelectionMode.None }))
    };

    foreach (var (name, render) in cases)
    {
        try
        {
            await render();
            Require(false, $"{name} must reject its invalid value instead of degrading silently.");
        }
        catch (ArgumentException) { }
    }
}

// REV-114: components whose parameter defaults used to be hard-coded English (or
// which refused an empty value outright) now fall back to the text table, so a
// host can localise them without setting every parameter.
async Task CheckTextTableFallbackAsync()
{
    var calendar = await RenderAsync<DateCalendar>(new Dictionary<string, object?>
    {
        [nameof(DateCalendar.DisplayMonth)] = new DateOnly(2026, 9, 1)
    });
    Require(calendar.Contains("aria-label=\"Calendar\"", StringComparison.Ordinal),
        "DateCalendar must fall back to the text table for its accessible name.");

    var deck = await RenderAsync<FlashCardGroup>(new Dictionary<string, object?>
    {
        ["ChildContent"] = (RenderFragment)(builder => builder.AddContent(0, "card"))
    });
    Require(deck.Contains("aria-label=\"Flash cards\"", StringComparison.Ordinal),
        "FlashCardGroup must fall back to the text table instead of throwing on an unset name.");

    var rating = await RenderAsync<AeterniUI.Components.Rating.Rating>(new Dictionary<string, object?>
    {
        ["Value"] = 3
    });
    Require(rating.Contains("aria-label=\"3 of 5\"", StringComparison.Ordinal),
        "Rating stars must announce their meaning through the text table, not a bare number.");

    var number = await RenderAsync<InputNumber<int>>(new Dictionary<string, object?>
    {
        ["Value"] = 1,
        ["ShowControls"] = true
    });
    Require(number.Contains("aria-label=\"Increase value\"", StringComparison.Ordinal)
        && number.Contains("aria-label=\"Decrease value\"", StringComparison.Ordinal),
        "InputNumber step buttons must fall back to the text table instead of hard-coded English.");

    var slider = await RenderAsync<Slider<int>>(new Dictionary<string, object?>
    {
        ["Value"] = 1
    });
    Require(slider.Contains("aria-label=\"Slider\"", StringComparison.Ordinal),
        "Slider must fall back to the text table for its accessible name.");
}

// Renders a component and hands back the instance, so a check can drive its
// private interaction path the way a real event would. Call from the dispatcher.
async Task<TComponent> RenderComponentAsync<TComponent>(Dictionary<string, object?> values)
    where TComponent : IComponent
{
    ParameterContractHost<TComponent> host = null!;
    await renderer.RenderComponentAsync<ParameterContractHost<TComponent>>(
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Ready"] = (Action<ParameterContractHost<TComponent>>)(x => host = x)
        }));
    host.Values = values;
    host.Update();
    return host.Inner;
}

// REV-128: the colour ramps are the theming substrate a host builds against, so
// the stops the library commits to are an API surface, not an implementation
// detail. The review found 190 tokens referenced nowhere and had to decide which
// were cruft and which were promise; this pins the answer so a stop cannot appear
// or vanish silently. The token layer carries the same statement as a comment.
async Task CheckTokenSurfaceAsync()
{
    var root = FindRepositoryRoot();
    var css = await File.ReadAllTextAsync(Path.Combine(root, "src/AeterniUI/wwwroot/css/aeterni_ui.css"));

    // Written out rather than generated: the sets are not arithmetic sequences
    // (they start at 50 and then step by 100), and the literal list *is* the
    // declaration this check exists to enforce.
    var tenStop = new[] { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900 };
    var expected = new Dictionary<string, int[]>(StringComparer.Ordinal)
    {
        ["brand"] = tenStop,
        ["danger"] = tenStop,
        ["info"] = tenStop,
        ["success"] = tenStop,
        ["warning"] = tenStop,
        // The neutral surfaces need both ends, so these two also carry 0 and 950.
        ["gray"] = [0, 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950],
        ["neutral"] = [0, 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950],
    };

    foreach (var (family, stops) in expected)
    {
        var declared = Regex.Matches(css, $@"^\s*--aeterni-{family}-(\d+)\s*:", RegexOptions.Multiline)
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(stop => stop)
            .ToArray();
        Require(declared.SequenceEqual(stops),
            $"The --aeterni-{family}-* ramp must declare exactly the committed stops: " +
            $"missing [{string.Join(", ", stops.Except(declared))}], unexpected [{string.Join(", ", declared.Except(stops))}].");
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

/// <summary>
/// Fails the first <c>import</c> and succeeds on every later one, so a test can
/// tell "the failure was retried" apart from "the failure was cached".
/// </summary>
internal sealed class FlakyImportJsRuntime : IJSRuntime
{
    public int ImportAttempts { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier != "import")
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        ImportAttempts++;
        if (ImportAttempts == 1)
        {
            throw new JSDisconnectedException("The circuit is mid-disconnect.");
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}
