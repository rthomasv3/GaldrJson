using System;

namespace GaldrJson
{
    /// <summary>
    /// Specifies the JSON property name for serialization and deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class GaldrJsonPropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GaldrJsonPropertyNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        public GaldrJsonPropertyNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
