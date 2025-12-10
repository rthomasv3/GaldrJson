// GaldrJson/ReferenceTracker.cs
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GaldrJson
{
    /// <summary>
    /// Tracks object references during serialization to detect circular references.
    /// </summary>
    public sealed class ReferenceTracker
    {
        private readonly HashSet<object> _references;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTracker"/> class.
        /// </summary>
        public ReferenceTracker()
        {
            _references = new HashSet<object>(new ReferenceEqualityComparer());
        }

        /// <summary>
        /// Attempts to push an object onto the tracking stack.
        /// Returns false if the object is already being serialized (cycle detected).
        /// </summary>
        public bool Push(object obj)
        {
            return _references.Add(obj);
        }

        /// <summary>
        /// Removes an object from the tracking stack after serialization completes.
        /// </summary>
        public void Pop(object obj)
        {
            _references.Remove(obj);
        }

        /// <summary>
        /// Custom equality comparer that uses reference equality instead of value equality.
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
