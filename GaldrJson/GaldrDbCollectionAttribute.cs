using System;

namespace GaldrJson
{
    /// <summary>
    /// Indicates that a type supports JSON serialization and represents a database collection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class GaldrDbCollectionAttribute : Attribute
    {
        /// <summary>
        /// Gets the optional collection name.
        /// </summary>
        public string CollectionName { get; }

        /// <summary>
        /// Initializes a new instance with default collection name.
        /// </summary>
        public GaldrDbCollectionAttribute()
        {
            CollectionName = null;
        }

        /// <summary>
        /// Initializes a new instance with the specified collection name.
        /// </summary>
        /// <param name="collectionName">The database collection name.</param>
        public GaldrDbCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
    }
}
