using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace GaldrJson.AspNetCore;

public static class GaldrJsonServiceCollectionExtensions
{
    public static IServiceCollection AddGaldrJson(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            // Add the factory (delegates to your registry)
            options.SerializerOptions.Converters.Add(new GaldrJsonConverterFactory());

            // Match your defaults (e.g., from ToCamelCase)
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = false; // Production default

            // Optional: Other STJ tweaks (e.g., ignore nulls if your gen supports)
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }
}
