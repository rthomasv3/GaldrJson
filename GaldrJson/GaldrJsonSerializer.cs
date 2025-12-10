using System;

namespace GaldrJson
{
    /// <summary>
    /// Provides methods for serializing and deserializing objects to and from JSON using the Galdr serialization
    /// framework.
    /// </summary>
    /// <remarks>
    /// GaldrJsonSerializer supports serialization and deserialization for types that are registered
    /// with the Galdr framework, typically by applying the [GaldrJsonSerializable] attribute. Attempting to serialize
    /// or deserialize unregistered types will result in a NotSupportedException or a failed operation.
    /// </remarks>
    [GaldrJsonIgnore]
    public class GaldrJsonSerializer : IGaldrJsonSerializer
    {
        /// <inheritdoc />
        public string Serialize<T>(T value, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            if (GaldrJsonSerializerRegistry.Serializer != null &&
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(typeof(T)))
            {
                return GaldrJsonSerializerRegistry.Serializer.Serialize(value, typeof(T), options);
            }

            throw new NotSupportedException($"Type {typeof(T).FullName} is not registered for serialization. Add [GaldrJsonSerializable] attribute to the type.");
        }

        /// <inheritdoc />
        public bool TrySerialize(object value, Type actualType, out string json, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            json = null;

            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(actualType))
            {
                json = GaldrJsonSerializerRegistry.Serializer.Serialize(value, actualType, options);
            }

            return json != null;
        }

        /// <inheritdoc />
        public bool TrySerialize<T>(T value, out string json, GaldrJsonOptions options = null)
        {
            return TrySerialize(value, typeof(T), out json, options);
        }

        /// <inheritdoc />
        public T Deserialize<T>(string json, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            if (GaldrJsonSerializerRegistry.Serializer != null &&
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(typeof(T)))
            {
                return (T)GaldrJsonSerializerRegistry.Serializer.Deserialize(json, typeof(T), options);
            }

            throw new NotSupportedException($"Type {typeof(T).FullName} is not registered for deserialization. Add [GaldrJsonSerializable] attribute to the type.");
        }

        /// <inheritdoc />
        public bool TryDeserialize(string json, Type targetType, out object value, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(targetType))
            {
                value = GaldrJsonSerializerRegistry.Serializer.Deserialize(json, targetType, options);
                return true;
            }

            value = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryDeserialize<T>(string json, out T value, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(typeof(T)))
            {
                value = (T)GaldrJsonSerializerRegistry.Serializer.Deserialize(json, typeof(T), options);
                return true;
            }

            value = default;
            return false;
        }
    }
}
