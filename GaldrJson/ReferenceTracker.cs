using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GaldrJson
{
    /// <summary>
    /// Tracks object references during serialization to detect circular references.
    /// Uses hash codes instead of storing object references to reduce memory pressure.
    /// </summary>
    public sealed class ReferenceTracker
    {
        [ThreadStatic]
        private static ReferenceTracker t_cached;

        private readonly HashSet<int> _hashCodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTracker"/> class.
        /// </summary>
        public ReferenceTracker()
        {
            _hashCodes = new HashSet<int>();
        }

        /// <summary>
        /// Rents a cached ReferenceTracker instance for the current thread.
        /// </summary>
        public static ReferenceTracker Rent()
        {
            ReferenceTracker tracker = t_cached;
            if (tracker == null)
            {
                tracker = new ReferenceTracker();
                t_cached = tracker;
            }
            tracker.Clear();
            return tracker;
        }

        /// <summary>
        /// Clears all tracked references.
        /// </summary>
        public void Clear()
        {
            _hashCodes.Clear();
        }

        /// <summary>
        /// Attempts to push an object onto the tracking stack.
        /// Returns false if the object is already being serialized (cycle detected).
        /// </summary>
        public bool Push(object obj)
        {
            if (obj == null)
                return true;

            int hashCode = RuntimeHelpers.GetHashCode(obj);
            return _hashCodes.Add(hashCode);
        }

        /// <summary>
        /// Removes an object from the tracking stack after serialization completes.
        /// </summary>
        public void Pop(object obj)
        {
            if (obj == null)
                return;

            int hashCode = RuntimeHelpers.GetHashCode(obj);
            _hashCodes.Remove(hashCode);
        }
    }
}
