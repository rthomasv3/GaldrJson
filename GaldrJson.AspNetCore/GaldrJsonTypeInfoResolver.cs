using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GaldrJson.AspNetCore;

/// <summary>
/// Supplies JsonTypeInfo metadata for GaldrJson-serializable types. Minimal APIs ask the
/// TypeInfoResolver for metadata before any converter is consulted, and that request fails
/// when the chain is empty - which it is under Native AOT (the SDK disables the reflection
/// resolver) and on WebApplication.CreateSlimBuilder. This resolver answers for registered
/// GaldrJson types so serialization reaches the GaldrJson converter.
/// </summary>
internal sealed class GaldrJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The created JsonTypeInfo is backed by GaldrJsonConverterFactory (registered on the same options), so no property metadata is ever reflected.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Only instantiates the JsonTypeInfo<T> shell; reference types use shared generic code under Native AOT. Struct root types are not covered and keep needing JsonSerializerIsReflectionEnabledByDefault.")]
    public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo = null;

        IGaldrJsonTypeSerializer serializer = GaldrJsonSerializerRegistry.Serializer;

        if (serializer != null && serializer.CanSerialize(type))
        {
            typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, options);
        }

        return typeInfo;
    }
}
