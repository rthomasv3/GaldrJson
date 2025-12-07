using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GaldrJson.AspNetCore;

internal class DelegatingJsonConverter : JsonConverter<object>
{
    #region Fields

    private readonly Type _typeToConvert;

    #endregion

    #region Constructor

    public DelegatingJsonConverter(Type typeToConvert)
    {
        _typeToConvert = typeToConvert;
    }

    #endregion

    #region Public Methods

    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        IGaldrJsonTypeSerializer serializer = GaldrJsonSerializerRegistry.Serializer;

        if (serializer == null || !serializer.CanSerialize(_typeToConvert))
        {
            throw new NotSupportedException($"Type {_typeToConvert.FullName} is not registered for deserialization. Add [GaldrJsonSerializable] attribute to the type.");
        }

        return serializer.Read(ref reader, _typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        IGaldrJsonTypeSerializer serializer = GaldrJsonSerializerRegistry.Serializer;

        if (serializer == null || !serializer.CanSerialize(_typeToConvert))
        {
            throw new NotSupportedException($"Type {value?.GetType().FullName ?? _typeToConvert.FullName} is not registered for serialization. Add [GaldrJsonSerializable] attribute to the type.");
        }

        serializer.Write(writer, value, _typeToConvert, options);
    }

    #endregion
}
