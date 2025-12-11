using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace GaldrJson.AspNetCore;

/// <summary>
/// Provides extension methods for registering Galdr JSON serialization services and configuration with an
/// IServiceCollection.
/// </summary>
/// <remarks>
/// This class contains extension methods that integrate Galdr JSON serialization into ASP.NET Core
/// dependency injection and HTTP JSON options.
/// </remarks>
public static class GaldrJsonServiceCollectionExtensions
{
    /// <summary>
    /// Adds Galdr JSON serialization services and configures System.Text.Json options for use with minimal APIs.
    /// </summary>
    /// <remarks>
    /// This method registers the Galdr JSON serializer and configures JSON options to use
    /// Galdr-specific converters and naming policies. Call this method during application startup to enable Galdr JSON
    /// support in your application's dependency injection container.
    /// </remarks>
    /// <param name="services">The service collection to which the Galdr JSON services will be added. Cannot be null.</param>
    /// <param name="galdrJsonOptions">Optional json serialization/deserialization options .</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    public static IServiceCollection AddGaldrJson(this IServiceCollection services, GaldrJsonOptions galdrJsonOptions = null)
    {
        services.AddSingleton<GaldrJsonSerializer>();
        services.AddSingleton<IGaldrJsonSerializer>(sp => sp.GetRequiredService<GaldrJsonSerializer>());

        services.ConfigureHttpJsonOptions(options =>
        {
            JsonNamingPolicy namingPolicy = null;
            if (galdrJsonOptions?.PropertyNamingPolicy == PropertyNamingPolicy.CamelCase)
            {
                namingPolicy = JsonNamingPolicy.CamelCase;
            }
            else if (galdrJsonOptions?.PropertyNamingPolicy == PropertyNamingPolicy.SnakeCase)
            {
                namingPolicy = JsonNamingPolicy.SnakeCaseLower;
            }
            else if (galdrJsonOptions?.PropertyNamingPolicy == PropertyNamingPolicy.KebabCase)
            {
                namingPolicy = JsonNamingPolicy.KebabCaseLower;
            }

            options.SerializerOptions.Converters.Add(new GaldrJsonConverterFactory());

            options.SerializerOptions.PropertyNamingPolicy = namingPolicy;
            options.SerializerOptions.WriteIndented = galdrJsonOptions?.WriteIndented ?? false;
            options.SerializerOptions.PropertyNameCaseInsensitive = galdrJsonOptions?.PropertyNameCaseInsensitive ?? false;
            options.SerializerOptions.ReferenceHandler = galdrJsonOptions?.DetectCycles == true ? ReferenceHandler.Preserve : null;
        });

        return services;
    }
}
