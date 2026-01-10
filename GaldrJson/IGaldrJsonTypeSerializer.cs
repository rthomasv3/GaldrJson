using System;
using System.Text.Json;

namespace GaldrJson
{
    /// <summary>
    /// Interface used to represent Galdr serialization features.
    /// </summary>
    public interface IGaldrJsonTypeSerializer
    {
        /// <summary>
        /// Serializes an object of the given type.
        /// </summary>
        string Serialize(object value, Type type, GaldrJsonOptions options);

        /// <summary>
        /// Deserializes an object of the given type.
        /// </summary>
        object Deserialize(string json, Type type, GaldrJsonOptions options);

        /// <summary>
        /// Returns true if the type can be serialized.
        /// </summary>
        bool CanSerialize(Type type);

        /// <summary>
        /// Writes the value using Utf8JsonWriter.
        /// </summary>
        void Write(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options, ReferenceTracker tracker);

        /// <summary>
        /// Reads the value using Utf8JsonReader.
        /// </summary>
        object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);

        /// <summary>
        /// Serializes an object directly to a Utf8JsonWriter.
        /// </summary>
        void SerializeTo(Utf8JsonWriter writer, object value, Type type, GaldrJsonOptions options);
    }
}
