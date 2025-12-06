using System;

namespace GaldrJson
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGaldrJsonSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        bool TrySerialize(object value, Type actualType, out string json);
        /// <summary>
        /// 
        /// </summary>
        bool TrySerialize<T>(T value, out string json);
        /// <summary>
        /// 
        /// </summary>
        bool TryDeserialize(string json, Type targetType, out object value);
        /// <summary>
        /// 
        /// </summary>
        bool TryDeserialize<T>(string json, out T value);
    }
}
