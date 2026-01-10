namespace GaldrJson.Tests
{
    [TestClass]
    public sealed class GaldrBufferWriterTests
    {
        [TestMethod]
        public void GaldrBufferWriter_BasicWriteAndRead()
        {
            var buffer = new GaldrBufferWriter(initialCapacity: 16);
            byte[] data = { 1, 2, 3, 4, 5 };

            var span = buffer.GetSpan(data.Length);
            data.CopyTo(span);
            buffer.Advance(data.Length);

            Assert.AreEqual(5, buffer.WrittenCount);
            Assert.IsTrue(buffer.WrittenSpan.SequenceEqual(data));
        }

        [TestMethod]
        public void GaldrBufferWriter_GrowsAutomatically()
        {
            var buffer = new GaldrBufferWriter(initialCapacity: 4);

            // Write more than initial capacity
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var span = buffer.GetSpan(data.Length);
            data.CopyTo(span);
            buffer.Advance(data.Length);

            Assert.AreEqual(10, buffer.WrittenCount);
            Assert.IsGreaterThanOrEqualTo(10, buffer.Capacity);
            Assert.IsTrue(buffer.WrittenSpan.SequenceEqual(data));
        }

        [TestMethod]
        public void GaldrBufferWriter_Clear_ResetsWrittenCount()
        {
            var buffer = new GaldrBufferWriter();
            var span = buffer.GetSpan(5);
            buffer.Advance(5);

            Assert.AreEqual(5, buffer.WrittenCount);

            buffer.Clear();

            Assert.AreEqual(0, buffer.WrittenCount);
        }

        [TestMethod]
        public void GaldrBufferWriter_GetMemory_ReturnsValidMemory()
        {
            var buffer = new GaldrBufferWriter();
            var memory = buffer.GetMemory(10);

            Assert.IsGreaterThanOrEqualTo(10, memory.Length);
        }

        [TestMethod]
        public void GaldrBufferWriter_InvalidInitialCapacity_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new GaldrBufferWriter(initialCapacity: 0));
        }

        [TestMethod]
        public void GaldrBufferWriter_NegativeAdvance_Throws()
        {
            var buffer = new GaldrBufferWriter();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Advance(-1));
        }

        [TestMethod]
        public void GaldrBufferWriter_AdvancePastEnd_Throws()
        {
            var buffer = new GaldrBufferWriter(initialCapacity: 10);
            Assert.ThrowsExactly<InvalidOperationException>(() => buffer.Advance(100));
        }
    }
}
