using System;

namespace GaldrJson
{
    /// <summary>
    /// Provides static methods for JSON serialization and deserialization.
    /// </summary>
    public static class GaldrJson
    {
        /// <summary>
        /// Serializes the specified value to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of the value to serialize.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">Optional serialization settings.</param>
        /// <returns>A JSON string representation of the value.</returns>
        /// <exception cref="NotSupportedException">Thrown when the type is not registered for serialization.</exception>
        public static string Serialize<T>(T value, GaldrJsonOptions options = null)
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

        /// <summary>
        /// Serializes the specified value to a JSON string using the actual runtime type.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="type">The type to use for serialization.</param>
        /// <param name="options">Optional serialization settings.</param>
        /// <returns>A JSON string representation of the value.</returns>
        /// <exception cref="NotSupportedException">Thrown when the type is not registered for serialization.</exception>
        public static string Serialize(object value, Type type, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            if (GaldrJsonSerializerRegistry.Serializer != null &&
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(type))
            {
                return GaldrJsonSerializerRegistry.Serializer.Serialize(value, type, options);
            }

            throw new NotSupportedException($"Type {type.FullName} is not registered for serialization. Add [GaldrJsonSerializable] attribute to the type.");
        }

        /// <summary>
        /// Deserializes the JSON string to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="options">Optional serialization settings.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="NotSupportedException">Thrown when the type is not registered for deserialization.</exception>
        public static T Deserialize<T>(string json, GaldrJsonOptions options = null)
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

        /// <summary>
        /// Deserializes the JSON string to the specified type.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="type">The type to deserialize to.</param>
        /// <param name="options">Optional serialization settings.</param>
        /// <returns>The deserialized value.</returns>
        /// <exception cref="NotSupportedException">Thrown when the type is not registered for deserialization.</exception>
        public static object Deserialize(string json, Type type, GaldrJsonOptions options = null)
        {
            if (options == null)
                options = GaldrJsonOptions.Default;

            if (GaldrJsonSerializerRegistry.Serializer != null &&
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(type))
            {
                return GaldrJsonSerializerRegistry.Serializer.Deserialize(json, type, options);
            }

            throw new NotSupportedException($"Type {type.FullName} is not registered for deserialization. Add [GaldrJsonSerializable] attribute to the type.");
        }
    }
}