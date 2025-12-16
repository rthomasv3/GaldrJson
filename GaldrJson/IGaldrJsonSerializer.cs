using System;

namespace GaldrJson
{
    /// <summary>
    /// Interface used to define the features of a Galdr Json Serializer.
    /// </summary>
    public interface IGaldrJsonSerializer
    {
        /// <summary>
        /// Serializes the specified value to a JSON string.
        /// </summary>
        string Serialize<T>(T value, GaldrJsonOptions options = null);

        /// <summary>
        /// Serializes the specified value to a JSON string.
        /// </summary>
        bool TrySerialize(object value, Type actualType, out string json, GaldrJsonOptions options = null);

        /// <summary>
        /// Serializes the specified value to a JSON string.
        /// </summary>
        bool TrySerialize<T>(T value, out string json, GaldrJsonOptions options = null);

        /// <summary>
        /// Deserializes the JSON string to the specified type.
        /// </summary>
        T Deserialize<T>(string json, GaldrJsonOptions options = null);

        /// <summary>
        /// Deserializes the JSON string to the specified type.
        /// </summary>
        bool TryDeserialize(string json, Type targetType, out object value, GaldrJsonOptions options = null);

        /// <summary>
        /// Deserializes the JSON string to the specified type.
        /// </summary>
        bool TryDeserialize<T>(string json, out T value, GaldrJsonOptions options = null);
    }
}
