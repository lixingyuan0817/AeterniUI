using System.Globalization;
using System.Text;
using AeterniUI.Services.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace AeterniUI.Services;

public static class AeterniServiceCollectionExtensions
{
    public static IServiceCollection AddAeterniUI(
        this IServiceCollection services,
        Action<AeterniUIOptions>? configure = null)
    {
        var options = new AeterniUIOptions();
        configure?.Invoke(options);

        if (!Enum.IsDefined(options.DefaultToastPosition))
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultToastPosition));
        }

        if (!Enum.IsDefined(options.DefaultAlertPosition))
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultAlertPosition));
        }

        if (!Enum.IsDefined(options.DefaultBrand))
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultBrand));
        }

        if (options.MaxToastCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxToastCount));
        }

        ValidateDuration(options.DefaultAlertDuration, nameof(options.DefaultAlertDuration));
        ValidateDuration(options.DefaultToastDuration, nameof(options.DefaultToastDuration));

        // Empty text entries fall back to the English defaults instead of
        // rendering an unlabelled control.
        options.Text.FillEmptyFrom(new AeterniUITextOptions());
        ValidateCompositeFormat(options.Text.PaginationPageLabelFormat, nameof(options.Text.PaginationPageLabelFormat));

        services.AddSingleton(options);
        services.AddScoped<JsModuleManager>();
        services.AddScoped<ThemeService>();
        services.AddScoped<DialogService>();
        services.AddScoped<IDialogService>(sp => sp.GetRequiredService<DialogService>());
        return services;
    }

    private static void ValidateDuration(TimeSpan duration, string parameterName)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The default duration cannot be negative.");
        }
    }

    private static void ValidateCompositeFormat(string format, string parameterName)
    {
        try
        {
            var parsed = CompositeFormat.Parse(format);
            if (parsed.MinimumArgumentCount != 1)
            {
                throw new FormatException("The format must reference page number placeholder {0} and no higher argument index.");
            }

            _ = string.Format(CultureInfo.InvariantCulture, parsed, 1);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The text entry must be a valid composite format that accepts one value as {0}.", parameterName, exception);
        }
    }
}
