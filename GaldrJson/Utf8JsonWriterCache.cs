using System;
using System.Text.Json;

namespace GaldrJson
{
    /// <summary>
    /// Provides thread-local caching of Utf8JsonWriter instances to reduce allocations.
    /// </summary>
    public static class Utf8JsonWriterCache
    {
        [ThreadStatic]
        private static CachedWriter t_cachedWriterIndented;

        [ThreadStatic]
        private static CachedWriter t_cachedWriterNotIndented;

        private sealed class CachedWriter
        {
            public GaldrBufferWriter BufferWriter { get; }
            public Utf8JsonWriter Writer { get; }

            public CachedWriter(bool indented)
            {
                BufferWriter = new GaldrBufferWriter(initialCapacity: 16384);
                Writer = new Utf8JsonWriter(BufferWriter, new JsonWriterOptions { Indented = indented });
            }

            public void Reset()
            {
                BufferWriter.Clear();
                Writer.Reset(BufferWriter);
            }
        }

        /// <summary>
        /// Rents a cached Utf8JsonWriter for the current thread.
        /// </summary>
        /// <param name="indented">Whether the writer should produce indented output.</param>
        /// <param name="bufferWriter">The underlying buffer writer.</param>
        /// <returns>A cached Utf8JsonWriter instance.</returns>
        public static Utf8JsonWriter RentWriter(bool indented, out GaldrBufferWriter bufferWriter)
        {
            CachedWriter cached;

            if (indented)
            {
                cached = t_cachedWriterIndented;
                if (cached == null)
                {
                    cached = new CachedWriter(indented: true);
                    t_cachedWriterIndented = cached;
                }
            }
            else
            {
                cached = t_cachedWriterNotIndented;
                if (cached == null)
                {
                    cached = new CachedWriter(indented: false);
                    t_cachedWriterNotIndented = cached;
                }
            }

            cached.Reset();
            bufferWriter = cached.BufferWriter;
            return cached.Writer;
        }
    }
}
