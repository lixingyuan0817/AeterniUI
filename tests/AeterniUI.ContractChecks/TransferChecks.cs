using System.Reflection;
using System.Text.RegularExpressions;
using AeterniUI.Components.Transfer;
using AeterniUI.Enums;
using AeterniUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Transfer 契约检查：根 group / 两个 listbox 的角色层级、aria-selected 字符串、
/// 列标题文案、尺寸枚举校验、SelectedValues 子集剔除、迁移链路与搜索过滤。
/// </summary>
internal static class TransferChecks
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static async Task RunAsync(HtmlRenderer renderer, List<string> failures)
    {
        void Require(bool condition, string message)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            // Defaults come from the text table, not from hard-coded literals, so this
            // check also guards the "instance parameter wins over the table" contract.
            var text = new AeterniUITextOptions();

            async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
                where TComponent : IComponent
            {
                var root = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
                return root.ToHtmlString();
            }

            async Task<(TComponent Inner, ParameterContractHost<TComponent> Host, Func<string> Html)> RenderInstanceAsync<TComponent>(
                Dictionary<string, object?> parameters)
                where TComponent : IComponent
            {
                ParameterContractHost<TComponent> host = null!;
                var root = await renderer.RenderComponentAsync<ParameterContractHost<TComponent>>(
                    ParameterView.FromDictionary(new Dictionary<string, object?>
                    {
                        ["Ready"] = (Action<ParameterContractHost<TComponent>>)(instance => host = instance)
                    }));
                host.Values = parameters;
                host.Update();
                return (host.Inner, host, root.ToHtmlString);
            }

            static async Task InvokeAsync(object instance, string name, params object?[] args)
            {
                var method = instance.GetType().GetMethod(name, InstanceFlags)
                    ?? throw new InvalidOperationException($"Transfer is missing the private member '{name}'.");
                if (method.Invoke(instance, args) is Task task)
                {
                    await task;
                }
            }

            static string Labels(IReadOnlyList<TransferItem> values) =>
                string.Join(",", values.Select(value => value.Label));

            static string ButtonTag(string html, string label) =>
                Regex.Match(html, $"<button[^>]*aria-label=\"{Regex.Escape(label)}\"[^>]*>").Value;

            // ---------------------------------------------------------- 静态渲染

            var html = await RenderAsync<Transfer<TransferItem>>(new Dictionary<string, object?>
            {
                [nameof(Transfer<TransferItem>.Id)] = "contract-transfer",
                [nameof(Transfer<TransferItem>.Class)] = "consumer-transfer",
                [nameof(Transfer<TransferItem>.Style)] = "margin: 0",
                [nameof(Transfer<TransferItem>.AdditionalAttributes)] =
                    new Dictionary<string, object> { ["data-contract"] = "transfer" },
                [nameof(Transfer<TransferItem>.Items)] = TransferFixtures.Items,
                [nameof(Transfer<TransferItem>.SelectedValues)] = TransferFixtures.InitialSelected,
                [nameof(Transfer<TransferItem>.TextSelector)] = TransferFixtures.Text,
                [nameof(Transfer<TransferItem>.DisabledSelector)] = TransferFixtures.Disabled,
                [nameof(Transfer<TransferItem>.Size)] = Size.Large,
                [nameof(Transfer<TransferItem>.Required)] = true,
                [nameof(Transfer<TransferItem>.Invalid)] = true,
                [nameof(Transfer<TransferItem>.SourceTitle)] = "Unassigned",
                [nameof(Transfer<TransferItem>.TargetTitle)] = "Assigned",
                [nameof(Transfer<TransferItem>.Searchable)] = true,
                [nameof(Transfer<TransferItem>.SearchPlaceholder)] = "Filter options",
                [nameof(Transfer<TransferItem>.AriaLabel)] = "Permissions"
            });

            var rootTag = Regex.Match(html, "<div[^>]*id=\"contract-transfer\"[^>]*>").Value;
            foreach (var value in new[]
            {
                "role=\"group\"", "aria-label=\"Permissions\"", "consumer-transfer", "margin: 0",
                "data-contract=\"transfer\"", "aeterni-transfer--lg", "is-invalid",
                "aria-invalid=\"true\""
            })
            {
                Require(rootTag.Contains(value, StringComparison.Ordinal),
                    $"Transfer root must render {value}.");
            }

            Require(Regex.Matches(html, "role=\"listbox\"").Count == 2,
                "Transfer must render exactly two listboxes, one per column.");
            Require(Regex.Matches(html, "aria-multiselectable=\"true\"").Count == 2,
                "Both Transfer listboxes must declare aria-multiselectable.");

            var sourceTag = Regex.Match(html, "<div[^>]*id=\"contract-transfer-source\"[^>]*>").Value;
            var targetTag = Regex.Match(html, "<div[^>]*id=\"contract-transfer-target\"[^>]*>").Value;
            Require(sourceTag.Contains("role=\"listbox\"")
                && sourceTag.Contains("aria-label=\"Unassigned\"")
                && sourceTag.Contains("data-side=\"source\"")
                && sourceTag.Contains("aria-required=\"true\"")
                && sourceTag.Contains("tabindex=\"0\""),
                "The source column must be a tabbable listbox named by SourceTitle.");
            Require(targetTag.Contains("role=\"listbox\"")
                && targetTag.Contains("aria-label=\"Assigned\"")
                && targetTag.Contains("data-side=\"target\"")
                && targetTag.Contains("tabindex=\"0\""),
                "The target column must be a tabbable listbox named by TargetTitle.");
            Require(!sourceTag.Contains("aria-activedescendant", StringComparison.Ordinal),
                "A listbox with no active option must not point aria-activedescendant anywhere.");
            Require(html.Contains(">Unassigned<", StringComparison.Ordinal)
                && html.Contains(">Assigned<", StringComparison.Ordinal),
                "Column titles must be visible text as well as accessible names.");

            var actionsStart = html.IndexOf("aeterni-transfer__actions", StringComparison.Ordinal);
            var sourceStart = html.IndexOf("data-side=\"source\"", StringComparison.Ordinal);
            var targetStart = html.IndexOf("data-side=\"target\"", StringComparison.Ordinal);
            Require(actionsStart > sourceStart && targetStart > actionsStart,
                "DOM order must be source column, action buttons, target column so Tab follows that order.");

            var sourcePart = html[sourceStart..actionsStart];
            var targetPart = html[targetStart..];
            Require(Regex.Matches(html, "role=\"option\"").Count == 4,
                "Transfer must render every candidate as an option: source items plus selected values.");
            Require(Regex.Matches(html, "aria-selected=\"false\"").Count == 4
                && !html.Contains("aria-selected=\"true\"", StringComparison.Ordinal),
                "Options must report aria-selected as a string, and checked state starts empty.");
            Require(sourcePart.Contains("Alpha", StringComparison.Ordinal)
                && sourcePart.Contains("Blocked", StringComparison.Ordinal)
                && !sourcePart.Contains("Beta", StringComparison.Ordinal)
                && !sourcePart.Contains("Gamma", StringComparison.Ordinal),
                "The source column must render only items that are not selected.");
            Require(sourcePart.Contains("aria-disabled=\"true\"", StringComparison.Ordinal),
                "A disabled candidate must be marked aria-disabled in the source column.");
            Require(targetPart.IndexOf("Gamma", StringComparison.Ordinal) >= 0
                && targetPart.IndexOf("Gamma", StringComparison.Ordinal) < targetPart.IndexOf("Beta", StringComparison.Ordinal),
                "The target column must follow the SelectedValues order.");
            Require(Regex.Matches(html, "<input[^>]*type=\"search\"[^>]*>").Count == 2
                && html.Contains("placeholder=\"Filter options\"", StringComparison.Ordinal),
                "Searchable must render one search input per column with the supplied placeholder.");

            var buttons = Regex.Matches(html, "<button[^>]*type=\"button\"[^>]*>");
            Require(buttons.Count == 4, "Transfer must render four native transfer buttons.");
            foreach (var label in new[]
            {
                text.TransferMoveSelectedLabel, text.TransferRemoveSelectedLabel,
                text.TransferMoveAllLabel, text.TransferRemoveAllLabel
            })
            {
                Require(ButtonTag(html, label).Length > 0,
                    $"Transfer must name its action button '{label}' from the text table.");
            }

            Require(ButtonTag(html, text.TransferMoveSelectedLabel).Contains("disabled", StringComparison.Ordinal),
                "The checked-to-target button must start disabled while nothing is checked.");
            Require(!ButtonTag(html, text.TransferMoveAllLabel).Contains("disabled", StringComparison.Ordinal),
                "The all-to-target button must be enabled while an enabled candidate remains.");

            Require(!Regex.IsMatch(html, ">[^<]*[\\u4e00-\\u9fff][^<]*<"),
                "Transfer must not render CJK text from built-in defaults.");
            Require(!html.Contains("}", StringComparison.Ordinal),
                "Transfer must not leak Razor closing braces into rendered markup.");

            // ---------------------------------------------------------- 尺寸与参数校验

            foreach (var size in Enum.GetValues<Size>())
            {
                var sized = await RenderAsync<Transfer<TransferItem>>(new Dictionary<string, object?>
                {
                    [nameof(Transfer<TransferItem>.Items)] = TransferFixtures.Items,
                    [nameof(Transfer<TransferItem>.Size)] = size
                });
                Require(sized.Contains("aeterni-transfer--sm") == (size == Size.Small)
                    && sized.Contains("aeterni-transfer--lg") == (size == Size.Large),
                    $"Transfer {size} must render only its matching size modifier.");
            }

            try
            {
                await RenderAsync<Transfer<TransferItem>>(new Dictionary<string, object?>
                {
                    [nameof(Transfer<TransferItem>.Items)] = TransferFixtures.Items,
                    [nameof(Transfer<TransferItem>.Size)] = (Size)99
                });
                Require(false, "Transfer must reject unknown sizes.");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                await RenderAsync<Transfer<TransferItem>>(new Dictionary<string, object?>
                {
                    [nameof(Transfer<TransferItem>.Items)] = null
                });
                Require(false, "Transfer must reject a null Items.");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                await RenderAsync<Transfer<TransferItem>>(new Dictionary<string, object?>
                {
                    [nameof(Transfer<TransferItem>.Items)] = TransferFixtures.Items,
                    [nameof(Transfer<TransferItem>.SelectedValues)] = null
                });
                Require(false, "Transfer must reject a null SelectedValues.");
            }
            catch (ArgumentNullException)
            {
            }

            // ---------------------------------------------------------- 参数同步：子集剔除

            TransferSubsetHost subsetHost = null!;
            var subsetRoot = await renderer.RenderComponentAsync<TransferSubsetHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Ready"] = (Action<TransferSubsetHost>)(instance => subsetHost = instance)
                }));
            var subsetHtml = subsetRoot.ToHtmlString();
            Require(subsetHost.Changes.Count == 1,
                $"A value outside Items must be dropped with one SelectedValuesChanged callback, got {subsetHost.Changes.Count}.");
            Require(subsetHost.Changes.Count == 1 && Labels(subsetHost.Changes[0]) == "Beta",
                "The callback must report the retained SelectedValues subset, not the original list.");
            Require(subsetHost.Values.Count == 1 && subsetHost.Values[0].Label == "Beta",
                "The component must write the retained subset back into SelectedValues.");
            Require(!subsetHtml.Contains("Stranger", StringComparison.Ordinal),
                "A value outside Items must not render in the target column.");

            // ---------------------------------------------------------- 交互链路

            TransferContractHost contractHost = null!;
            var contractRoot = await renderer.RenderComponentAsync<TransferContractHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Ready"] = (Action<TransferContractHost>)(instance => contractHost = instance)
                }));
            var transfer = contractHost.Inner;
            var contractHtml = contractRoot.ToHtmlString();
            Require(contractHtml.Contains(text.TransferEmptyText, StringComparison.Ordinal),
                "An empty target column must render the built-in empty text.");

            // 禁用项不能被勾选：Blocked 是源列的最后一项。
            await InvokeAsync(transfer, "HandleOptionClickAsync", "source", 3);
            contractHost.Update();
            Require(!contractRoot.ToHtmlString().Contains("aria-selected=\"true\"", StringComparison.Ordinal),
                "A disabled candidate must not become checked.");

            // 键盘进入：ArrowDown 落到首项，Space 勾选，↓ 再勾第二项。
            await InvokeAsync(transfer, "HandleKeyFromBrowserAsync", "source", "ArrowDown");
            contractHost.Update();
            var afterArrow = Regex.Match(contractRoot.ToHtmlString(), "<div[^>]*data-side=\"source\"[^>]*>").Value;
            Require(Regex.IsMatch(afterArrow, "aria-activedescendant=\"[^\"]+-source-option-0\""),
                "ArrowDown must move aria-activedescendant onto the first enabled source option.");

            await InvokeAsync(transfer, "HandleKeyFromBrowserAsync", "source", " ");
            await InvokeAsync(transfer, "HandleKeyFromBrowserAsync", "source", "ArrowDown");
            await InvokeAsync(transfer, "HandleKeyFromBrowserAsync", "source", " ");
            contractHost.Update();
            Require(Regex.Matches(contractRoot.ToHtmlString(), "aria-selected=\"true\"").Count == 2,
                "Space must toggle the checked state of the active option, twice for two rows.");

            await InvokeAsync(transfer, "MoveCheckedToTargetAsync");
            contractHost.Update();
            Require(contractHost.Changes.Count == 1 && Labels(contractHost.Changes[0]) == "Alpha,Beta",
                $"Move selected must append checked items to SelectedValues in source order, got [{string.Join(" | ", contractHost.Changes.Select(Labels))}].");
            Require(Regex.Matches(contractRoot.ToHtmlString(), "aria-selected=\"true\"").Count == 0,
                "A move must clear the transient checked state.");

            // 目标列勾选后搬回：目标只剩不在勾选里的 Beta。
            await InvokeAsync(transfer, "HandleOptionClickAsync", "target", 0);
            await InvokeAsync(transfer, "MoveCheckedToSourceAsync");
            contractHost.Update();
            Require(contractHost.Changes.Count == 2 && Labels(contractHost.Changes[1]) == "Beta",
                "Return selected must remove only the checked target values.");

            // 全部迁移：只搬当前可见且未禁用项，目标按追加顺序排列。
            await InvokeAsync(transfer, "MoveAllToTargetAsync");
            contractHost.Update();
            Require(contractHost.Changes.Count == 3 && Labels(contractHost.Changes[2]) == "Beta,Alpha,Gamma",
                $"Move all must append every enabled candidate in source order, got {Labels(contractHost.Changes[2])}.");

            await InvokeAsync(transfer, "MoveAllToSourceAsync");
            contractHost.Update();
            Require(contractHost.Changes.Count == 4 && contractHost.Changes[3].Count == 0,
                "Return all must clear every enabled target value.");

            await InvokeAsync(transfer, "MoveAllToTargetAsync");
            contractHost.Update();
            Require(contractHost.Changes.Count == 5 && Labels(contractHost.Changes[4]) == "Alpha,Beta,Gamma",
                $"Move all must never move a disabled item, got {Labels(contractHost.Changes[4])}.");

            // ---------------------------------------------------------- 搜索过滤

            var (filterTransfer, filterHost, filterHtml) = await RenderInstanceAsync<Transfer<TransferItem>>(
                new Dictionary<string, object?>
                {
                    [nameof(Transfer<TransferItem>.Items)] = TransferFixtures.Items,
                    [nameof(Transfer<TransferItem>.SelectedValues)] = Array.Empty<TransferItem>(),
                    [nameof(Transfer<TransferItem>.TextSelector)] = TransferFixtures.Text,
                    [nameof(Transfer<TransferItem>.DisabledSelector)] = TransferFixtures.Disabled,
                    [nameof(Transfer<TransferItem>.Searchable)] = true,
                    [nameof(Transfer<TransferItem>.AriaLabel)] = "Permissions"
                });

            await InvokeAsync(filterTransfer, "HandleOptionClickAsync", "source", 0);
            filterHost.Update();
            Require(filterHtml().Contains("aria-selected=\"true\"", StringComparison.Ordinal),
                "The filter fixture must start with Alpha checked.");

            await InvokeAsync(filterTransfer, "HandleFilterAsync", "source", new ChangeEventArgs { Value = "ALP" });
            filterHost.Update();
            var filtered = filterHtml();
            var filteredSource = filtered[filtered.IndexOf("data-side=\"source\"", StringComparison.Ordinal)..];
            Require(filteredSource.Contains("Alpha", StringComparison.Ordinal)
                && !filteredSource.Contains("Beta", StringComparison.Ordinal)
                && !filteredSource.Contains("Gamma", StringComparison.Ordinal)
                && !filteredSource.Contains("Blocked", StringComparison.Ordinal),
                "The column filter must match case-insensitively and hide non-matching rows.");
            Require(!filtered.Contains("aria-selected=\"true\"", StringComparison.Ordinal),
                "Changing the filter must clear that column's checked set.");
            Require(filtered.Contains("value=\"ALP\"", StringComparison.Ordinal),
                "The search input must reflect the typed filter value.");
            Require(filtered.Contains(text.TransferEmptyText, StringComparison.Ordinal),
                "Filtering must not touch SelectedValues, so the empty target column stays empty.");

            // ---------------------------------------------------------- 表单字段级联

            TransferFormHost formHost = null!;
            var formRoot = await renderer.RenderComponentAsync<TransferFormHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Ready"] = (Action<TransferFormHost>)(instance => formHost = instance)
                }));
            formHost.Disabled = true;
            formHost.Required = true;
            formHost.Invalid = true;
            formHost.Update();

            var formHtml = formRoot.ToHtmlString();
            var formRootTag = Regex.Match(formHtml, "<div[^>]*class=\"aeterni-transfer[^\"]*\"[^>]*>").Value;
            Require(formRootTag.Contains("is-disabled", StringComparison.Ordinal)
                && formRootTag.Contains("is-invalid", StringComparison.Ordinal)
                && formRootTag.Contains("aria-disabled=\"true\"", StringComparison.Ordinal)
                && formRootTag.Contains("aria-invalid=\"true\"", StringComparison.Ordinal),
                "A Transfer inside a FormField must adopt the cascaded disabled/required/invalid state.");
            Require(Regex.Matches(formHtml, "<div[^>]*role=\"listbox\"[^>]*aria-required=\"true\"").Count == 2,
                "A required Transfer must mark both listboxes aria-required.");
            Require(formRootTag.Contains("aria-labelledby=", StringComparison.Ordinal)
                && !formRootTag.Contains("aria-label=", StringComparison.Ordinal),
                "A FormField label must name the group through aria-labelledby instead of a duplicate aria-label.");
            Require(formRootTag.Contains("aria-describedby=\"", StringComparison.Ordinal)
                && formRootTag.Contains("-description", StringComparison.Ordinal),
                "The form field description must be merged into the group's aria-describedby.");
            Require(Regex.Matches(formHtml, "<div[^>]*role=\"listbox\"[^>]*tabindex=\"-1\"").Count == 2,
                "A disabled Transfer must take both listboxes out of the Tab order.");
            Require(Regex.Matches(formHtml, "<button[^>]*type=\"button\"[^>]*disabled").Count == 4,
                "A disabled Transfer must disable all four transfer buttons.");
        });
    }
}
