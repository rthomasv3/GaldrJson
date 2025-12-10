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
        private readonly HashSet<int> _hashCodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTracker"/> class.
        /// </summary>
        public ReferenceTracker()
        {
            _hashCodes = new HashSet<int>();
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
