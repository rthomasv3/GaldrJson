using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Caches TypeMetadata instances to avoid recreating metadata for the same type symbols.
    /// This improves performance during type discovery and code generation.
    /// </summary>
    internal sealed class TypeMetadataCache
    {
        private readonly Dictionary<ITypeSymbol, TypeMetadata> _cache;

        public TypeMetadataCache()
        {
            // Use SymbolEqualityComparer to properly compare type symbols
            _cache = new Dictionary<ITypeSymbol, TypeMetadata>(SymbolEqualityComparer.Default);
        }

        /// <summary>
        /// Gets or creates TypeMetadata for the given type symbol.
        /// If metadata already exists in the cache, returns the cached instance.
        /// Otherwise, creates new metadata and adds it to the cache.
        /// </summary>
        public TypeMetadata GetOrCreate(ITypeSymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (!_cache.TryGetValue(symbol, out var metadata))
            {
                metadata = TypeMetadata.Create(symbol, this);
                _cache[symbol] = metadata;
            }

            return metadata;
        }

        /// <summary>
        /// Checks if metadata for the given type symbol exists in the cache.
        /// </summary>
        public bool Contains(ITypeSymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            return _cache.ContainsKey(symbol);
        }

        /// <summary>
        /// Gets the number of type metadata entries in the cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Clears all cached metadata.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }
}
