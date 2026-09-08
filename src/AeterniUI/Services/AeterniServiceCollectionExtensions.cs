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

        if (options.MaxToastCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxToastCount));
        }

        ValidateDuration(options.DefaultAlertDuration, nameof(options.DefaultAlertDuration));
        ValidateDuration(options.DefaultToastDuration, nameof(options.DefaultToastDuration));

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
}
