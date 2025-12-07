using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GaldrJson.AspNetCore;

internal class GaldrJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return !IsBasicType(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new DelegatingJsonConverter(typeToConvert);
    }

    private static bool IsBasicType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
            type == typeof(Guid) || type.IsEnum)
            return true;

        // Nullable basics
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return IsBasicType(underlying);

        // Arrays of basics
        if (type.IsArray)
            return IsBasicType(type.GetElementType()!);

        // Collections of basics (List<T>, etc.)
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(List<>) || def == typeof(IEnumerable<>) || def == typeof(ICollection<>))
                return IsBasicType(type.GetGenericArguments()[0]);

            // String-keyed dicts of basics
            if (def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>))
            {
                var args = type.GetGenericArguments();
                return args[0] == typeof(string) && IsBasicType(args[1]);
            }
        }

        return false;
    }
}
