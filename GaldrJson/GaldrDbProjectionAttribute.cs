using System;

namespace GaldrJson
{
    /// <summary>
    /// Indicates that a type is a projection (subset) of another type and supports JSON serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class GaldrDbProjectionAttribute : Attribute
    {
        /// <summary>
        /// Gets the type this class is a projection of.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Initializes a new instance with the specified source type.
        /// </summary>
        /// <param name="sourceType">The type this projection is based on.</param>
        public GaldrDbProjectionAttribute(Type sourceType)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        }
    }
}
