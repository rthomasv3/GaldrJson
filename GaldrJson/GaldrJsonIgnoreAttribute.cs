using System;

namespace GaldrJson
{
    /// <summary>
    /// Indicates that the property or field should be ignored during serialization and deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class GaldrJsonIgnoreAttribute : Attribute
    {
    }
}
