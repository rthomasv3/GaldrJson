namespace GaldrJson.Tests
{
    [TestClass]
    public sealed class ReferenceTrackerTests
    {
        [TestMethod]
        public void Rent_ReturnsCachedInstance()
        {
            var tracker1 = ReferenceTracker.Rent();
            var tracker2 = ReferenceTracker.Rent();

            // Should be the same instance on the same thread
            Assert.AreSame(tracker1, tracker2);
        }

        [TestMethod]
        public void Rent_ClearsState()
        {
            var tracker = ReferenceTracker.Rent();
            var obj = new object();

            Assert.IsTrue(tracker.Push(obj));
            tracker.Pop(obj);

            // Rent again - should be cleared
            var tracker2 = ReferenceTracker.Rent();
            Assert.IsTrue(tracker2.Push(obj)); // Should succeed since state was cleared
        }

        [TestMethod]
        public void Push_ReturnsTrueForNewObject()
        {
            var tracker = ReferenceTracker.Rent();
            var obj = new object();

            Assert.IsTrue(tracker.Push(obj));
        }

        [TestMethod]
        public void Push_ReturnsFalseForDuplicateObject()
        {
            var tracker = ReferenceTracker.Rent();
            var obj = new object();

            Assert.IsTrue(tracker.Push(obj));
            Assert.IsFalse(tracker.Push(obj)); // Same object again
        }

        [TestMethod]
        public void Pop_AllowsObjectToBeReAdded()
        {
            var tracker = ReferenceTracker.Rent();
            var obj = new object();

            Assert.IsTrue(tracker.Push(obj));
            tracker.Pop(obj);
            Assert.IsTrue(tracker.Push(obj)); // Should work after pop
        }

        [TestMethod]
        public void Push_HandlesNullGracefully()
        {
            var tracker = ReferenceTracker.Rent();

            Assert.IsTrue(tracker.Push(null)); // null should be allowed
        }

        [TestMethod]
        public void Pop_HandlesNullGracefully()
        {
            var tracker = ReferenceTracker.Rent();

            tracker.Pop(null); // Should not throw
        }

        [TestMethod]
        public void Clear_RemovesAllTrackedReferences()
        {
            var tracker = ReferenceTracker.Rent();
            var obj1 = new object();
            var obj2 = new object();

            tracker.Push(obj1);
            tracker.Push(obj2);
            tracker.Clear();

            // Both should be pushable again after clear
            Assert.IsTrue(tracker.Push(obj1));
            Assert.IsTrue(tracker.Push(obj2));
        }
    }
}
