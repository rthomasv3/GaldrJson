using System;
using System.Buffers;

namespace GaldrJson
{
    /// <summary>
    /// A simple growable buffer writer for byte sequences.
    /// </summary>
    public sealed class GaldrBufferWriter : IBufferWriter<byte>
    {
        private const int DefaultMinimumGrowth = 256;
        private const int MaxArraySize = 0x7FFFFFC7; // Array.MaxLength equivalent

        private byte[] _buffer;
        private int _written;

        /// <summary>
        /// Initializes a new instance with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial buffer capacity.</param>
        public GaldrBufferWriter(int initialCapacity = 16384)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            _buffer = new byte[initialCapacity];
            _written = 0;
        }

        /// <summary>
        /// Gets the number of bytes written to the buffer.
        /// </summary>
        public int WrittenCount => _written;

        /// <summary>
        /// Gets the total capacity of the buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Gets a span over the written portion of the buffer.
        /// </summary>
        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

        /// <summary>
        /// Gets a memory over the written portion of the buffer.
        /// </summary>
        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

        /// <summary>
        /// Clears the written data, resetting the buffer for reuse.
        /// </summary>
        public void Clear()
        {
            _written = 0;
        }

        /// <inheritdoc />
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (_written > _buffer.Length - count)
                throw new InvalidOperationException("Cannot advance past the end of the buffer.");

            _written += count;
        }

        /// <inheritdoc />
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_written);
        }

        /// <inheritdoc />
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_written);
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint <= 0)
                sizeHint = DefaultMinimumGrowth;

            int available = _buffer.Length - _written;
            if (available >= sizeHint)
                return;

            int needed = _written + sizeHint;
            int newSize = _buffer.Length;

            while (newSize < needed)
            {
                if (newSize > MaxArraySize / 2)
                {
                    newSize = needed > MaxArraySize ? throw new OutOfMemoryException() : MaxArraySize;
                    break;
                }
                newSize *= 2;
            }

            byte[] newBuffer = new byte[newSize];
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _written);
            _buffer = newBuffer;
        }
    }
}
