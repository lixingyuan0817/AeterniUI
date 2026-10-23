using System.Reflection;
using System.Text.RegularExpressions;
using AeterniUI.Components.Dialog;
using AeterniUI.Components.FormDialog;
using AeterniUI.Models.Dialog;
using AeterniUI.Services;
using AeterniUI.Services.Impl;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>
/// Contract checks for <c>FormDialog</c>, <c>FormDialogOptions</c> and the
/// provider-rendered form chrome (10.42.0).
/// </summary>
internal static class FormDialogChecks
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

        RenderFragment FormContent() => builder => builder.AddContent(0, "form-field-marker");

        // --- Result factories ---------------------------------------------------
        var success = FormDialogSubmitResult.Success("payload");
        Require(success.Succeeded && success.Message is null && success.Data is "payload",
            "FormDialogSubmitResult.Success must carry its payload and no message.");

        var failure = FormDialogSubmitResult.Fail("  Name is required.  ");
        Require(!failure.Succeeded && failure.Data is null && failure.Message == "Name is required.",
            "FormDialogSubmitResult.Fail must trim and expose the message.");

        foreach (var blank in new[] { "", "   " })
        {
            var threw = false;
            try
            {
                FormDialogSubmitResult.Fail(blank);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Require(threw, "FormDialogSubmitResult.Fail must reject an empty message; a silent failure looks like a dead button.");
        }

        // --- Options defaults ---------------------------------------------------
        var defaults = new FormDialogOptions();
        Require(defaults.SubmitText is null
            && defaults.CancelText is null
            && defaults.OnSubmit is null
            && defaults.OnCancel is null
            && defaults.AutofocusFirstField
            && defaults.SubmitOnEnter
            && defaults.ValidationMessage is null
            && defaults.Footer is null
            && defaults.ShowCloseButton
            && defaults.CloseOnEscape,
            "FormDialogOptions must default to focus-first-field, Enter-to-submit, no validation message and no host footer.");

        // --- Declarative component defaults ------------------------------------
        var component = new FormDialog();
        Require(!component.Open
            && component.AutofocusFirstField
            && component.SubmitOnEnter
            && component.CloseOnEscape
            && component.Title is null
            && component.ChildContent is null,
            "FormDialog must default to a closed dialog that autofocuses and submits on Enter.");

        // A closed declarative host renders nothing at all: it is a declaration
        // point for the provider-owned modal, not a wrapper element.
        var closedFormDialog = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<FormDialog>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(FormDialog.Open)] = false,
                [nameof(FormDialog.Title)] = "Rename project",
                [nameof(FormDialog.ChildContent)] = (RenderFragment)(builder => builder.AddContent(0, "closed-form-marker"))
            }));

            return output.ToHtmlString();
        });

        Require(string.IsNullOrWhiteSpace(closedFormDialog) && !closedFormDialog.Contains("closed-form-marker", StringComparison.Ordinal),
            "A closed FormDialog must not render visible content; it only forwards its content to the provider.");

        // Renders the provider against a fresh container so the public service path
        // is the one under test, exactly like the existing Dialog/Confirm checks.
        async Task<string> RenderDialogHtmlAsync(
            Action<IDialogService> arrange,
            Action<AeterniUIOptions>? configure = null)
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
            arrange(currentProvider.GetRequiredService<IDialogService>());

            await using var currentRenderer = new HtmlRenderer(
                currentProvider,
                currentProvider.GetRequiredService<ILoggerFactory>());

            return await currentRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await currentRenderer.RenderComponentAsync<DialogProvider>(ParameterView.Empty);
                return output.ToHtmlString();
            });
        }

        // --- Default form chrome ------------------------------------------------
        var html = await RenderDialogHtmlAsync(dialogs =>
        {
            _ = dialogs.ShowForm(FormContent(), new FormDialogOptions { Title = "Rename project" });
        });

        Require(html.Contains("data-aeterni-form-dialog=\"true\"", StringComparison.Ordinal),
            "ShowForm must render its body with the form marker so the JS module can distinguish it.");
        Require(html.Contains("data-aeterni-submit-on-enter=\"true\"", StringComparison.Ordinal)
            && html.Contains("data-aeterni-autofocus=\"true\"", StringComparison.Ordinal),
            "The form body must expose the SubmitOnEnter and AutofocusFirstField markers.");
        Require(html.Contains("form-field-marker", StringComparison.Ordinal),
            "ShowForm must render the supplied RenderFragment inside the dialog body.");
        Require(Regex.Matches(html, @">\s*Save\s*<").Count == 1
            && Regex.Matches(html, @">\s*Cancel\s*<").Count == 1,
            "An unset form footer must fall back to the text table's Save / Cancel labels.");
        Require(html.Contains("data-aeterni-form-dialog-submit=\"true\"", StringComparison.Ordinal),
            "The primary action must carry the data marker the Enter handler clicks.");
        Require(!Regex.IsMatch(html, @"data-aeterni-form-dialog-submit=""true""[^>]*\sdisabled"),
            "The primary action must not start disabled.");
        Require(html.Contains("aria-busy=\"false\"", StringComparison.Ordinal),
            "An idle form dialog must report aria-busy=\"false\" instead of omitting the state.");
        Require(!html.Contains("role=\"alert\"", StringComparison.Ordinal)
            && !html.Contains("aeterni-dialog-provider__validation", StringComparison.Ordinal),
            "An idle form dialog must not render an empty validation region.");

        // --- Host text / options overrides -------------------------------------
        var configuredHtml = await RenderDialogHtmlAsync(
            dialogs =>
            {
                _ = dialogs.ShowForm(FormContent(), new FormDialogOptions
                {
                    SubmitText = "Create",
                    CancelText = "Dismiss",
                    SubmitOnEnter = false,
                    AutofocusFirstField = false
                });
            },
            options => options.Text.FormDialogSubmitLabel = "保存");

        Require(configuredHtml.Contains(">Create<", StringComparison.Ordinal)
            && configuredHtml.Contains(">Dismiss<", StringComparison.Ordinal),
            "Explicit SubmitText / CancelText must win over the text table.");
        var encodedLocalized = System.Text.Encodings.Web.HtmlEncoder.Default.Encode("保存");
        Require(!configuredHtml.Contains($">{encodedLocalized}<", StringComparison.Ordinal),
            "An explicit submit label must replace the localised text-table label instead of rendering both.");
        Require(configuredHtml.Contains("data-aeterni-submit-on-enter=\"false\"", StringComparison.Ordinal)
            && configuredHtml.Contains("data-aeterni-autofocus=\"false\"", StringComparison.Ordinal),
            "SubmitOnEnter / AutofocusFirstField must reach the form body markers.");

        // --- Text table fallback for both actions ------------------------------
        var localizedHtml = await RenderDialogHtmlAsync(
            dialogs => _ = dialogs.ShowForm(FormContent(), new FormDialogOptions()),
            options =>
            {
                options.Text.FormDialogSubmitLabel = "保存";
                options.Text.FormDialogCancelLabel = "返回";
            });

        var encodedSubmit = System.Text.Encodings.Web.HtmlEncoder.Default.Encode("保存");
        var encodedCancel = System.Text.Encodings.Web.HtmlEncoder.Default.Encode("返回");
        Require(localizedHtml.Contains($">{encodedSubmit}<", StringComparison.Ordinal)
            && localizedHtml.Contains($">{encodedCancel}<", StringComparison.Ordinal),
            "The form footer labels must come from the text table so a localised host gets them.");

        // --- Non-form dialogs are untouched ------------------------------------
        var confirmHtml = await RenderDialogHtmlAsync(dialogs => _ = dialogs.ConfirmAsync("Discard the draft?"));
        Require(!confirmHtml.Contains("data-aeterni-form-dialog", StringComparison.Ordinal)
            && !confirmHtml.Contains("aria-busy", StringComparison.Ordinal)
            && !confirmHtml.Contains("aeterni-dialog-provider__validation", StringComparison.Ordinal),
            "Non-form dialogs must not gain form markers, aria-busy or the validation region.");

        // --- Busy state and validation region, driven through the service -----
        // The provider's buttons call the internal submit funnel, so the check drives
        // the same funnel reflectively (HtmlRenderer cannot click) and renders the
        // provider while a submission is paused mid-flight.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddAeterniUI();
        await using var formProvider = services.BuildServiceProvider();
        var formDialogs = (DialogService)formProvider.GetRequiredService<IDialogService>();

        var submitGate = new TaskCompletionSource<FormDialogSubmitResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var busyOptions = new FormDialogOptions { OnSubmit = () => submitGate.Task };
        var busyReference = formDialogs.ShowForm(FormContent(), busyOptions);

        var dialogsProperty = typeof(DialogService).GetProperty("Dialogs", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var entries = ((System.Collections.IEnumerable)dialogsProperty.GetValue(formDialogs)!).Cast<object>().ToArray();
        Require(entries.Length == 1, "ShowForm must push exactly one entry onto the dialog stack.");
        var submitForm = typeof(DialogService).GetMethod("SubmitFormAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var submitTask = (Task)submitForm.Invoke(formDialogs, [entries[0]])!;

        await using var formRenderer = new HtmlRenderer(
            formProvider,
            formProvider.GetRequiredService<ILoggerFactory>());

        Task<string> RenderFormProviderAsync() => formRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await formRenderer.RenderComponentAsync<DialogProvider>(ParameterView.Empty);
            return output.ToHtmlString();
        });

        var busyHtml = await RenderFormProviderAsync();
        Require(busyHtml.Contains("aria-busy=\"true\"", StringComparison.Ordinal),
            "A submitting form dialog must report aria-busy=\"true\".");
        Require(Regex.Matches(busyHtml, @"\sdisabled(=|\s|>)").Count >= 2,
            "While submitting, the cancel action and the close button must both be disabled.");
        Require(busyHtml.Contains("is-loading", StringComparison.Ordinal),
            "The submit action must show the shared loading state while OnSubmit runs.");

        submitGate.SetResult(FormDialogSubmitResult.Fail("Name is required."));
        await submitTask.WaitAsync(TimeSpan.FromSeconds(5));

        var failedHtml = await RenderFormProviderAsync();
        Require(failedHtml.Contains("role=\"alert\"", StringComparison.Ordinal)
            && failedHtml.Contains("Name is required.", StringComparison.Ordinal),
            "A failed submission must render its message in a role=\"alert\" region.");
        Require(failedHtml.Contains("aria-busy=\"false\"", StringComparison.Ordinal),
            "The busy state must reset after a failed submission.");
        Require(!failedHtml.Contains("is-loading", StringComparison.Ordinal),
            "The submit action must leave its loading state after a failed submission.");
        Require(!busyReference.Result.IsCompleted,
            "A failed submission must keep the dialog open so the user can correct the form.");

        // --- Successful submission closes with its payload ---------------------
        var successGate = new TaskCompletionSource<FormDialogSubmitResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var successReference = formDialogs.ShowForm(FormContent(), new FormDialogOptions
        {
            OnSubmit = () => successGate.Task,
            CancelText = "Discard"
        });

        var successEntries = ((System.Collections.IEnumerable)dialogsProperty.GetValue(formDialogs)!).Cast<object>().ToArray();
        Require(successEntries.Length == 2, "ShowForm must stack a second dialog while the first one is open.");
        var successSubmitTask = (Task)submitForm.Invoke(formDialogs, [successEntries[^1]])!;

        successGate.SetResult(FormDialogSubmitResult.Success("payload"));
        // The service retires a closing entry when the provider reports the measured
        // exit animation; HtmlRenderer never runs that hook, so report it here.
        var signalExit = typeof(DialogService).GetMethod("SignalExit", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await Task.Delay(50);
        signalExit.Invoke(formDialogs, [successReference.Id]);
        var successResult = await successReference.Result.WaitAsync(TimeSpan.FromSeconds(5));
        await successSubmitTask.WaitAsync(TimeSpan.FromSeconds(5));

        Require(successResult.Confirmed && (string?)successResult.Data == "payload",
            "A successful submission must close the dialog with DialogResult.Confirm(result.Data).");
    }
}
