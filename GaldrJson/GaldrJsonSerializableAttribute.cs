using System;

namespace GaldrJson
{
    /// <summary>
    /// Indicates that a type supports JSON serialization within the <see cref="GaldrJsonSerializerRegistry"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class GaldrJsonSerializableAttribute : Attribute
    {

    }
}
