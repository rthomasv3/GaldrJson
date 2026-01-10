using System.Text;

namespace GaldrJson.Tests
{
    [TestClass]
    public sealed class Utf8JsonWriterCacheTests
    {
        [TestMethod]
        public void RentWriter_ReturnsValidWriter()
        {
            var writer = Utf8JsonWriterCache.RentWriter(false, out var buffer);

            Assert.IsNotNull(writer);
            Assert.IsNotNull(buffer);
            Assert.AreEqual(0, buffer.WrittenCount);
        }

        [TestMethod]
        public void RentWriter_Indented_ReturnsIndentedWriter()
        {
            var writer = Utf8JsonWriterCache.RentWriter(true, out var buffer);

            writer.WriteStartObject();
            writer.WriteString("key", "value");
            writer.WriteEndObject();
            writer.Flush();

            string json = Encoding.UTF8.GetString(buffer.WrittenSpan);

            // Indented output should contain newlines
            Assert.IsTrue(json.Contains("\n") || json.Contains("\r"));
        }

        [TestMethod]
        public void RentWriter_NotIndented_ReturnsCompactWriter()
        {
            var writer = Utf8JsonWriterCache.RentWriter(false, out var buffer);

            writer.WriteStartObject();
            writer.WriteString("key", "value");
            writer.WriteEndObject();
            writer.Flush();

            string json = Encoding.UTF8.GetString(buffer.WrittenSpan);

            // Compact output should not contain newlines
            Assert.DoesNotContain("\n", json);
            Assert.DoesNotContain("\r", json);
        }

        [TestMethod]
        public void MultipleRents_ReusesSameBuffer()
        {
            var writer1 = Utf8JsonWriterCache.RentWriter(false, out var buffer1);
            writer1.WriteStartObject();
            writer1.WriteEndObject();
            writer1.Flush();
            int capacity1 = buffer1.Capacity;

            // Rent again - should reuse the same cached buffer
            var writer2 = Utf8JsonWriterCache.RentWriter(false, out var buffer2);

            // Buffer should be cleared but same capacity
            Assert.AreEqual(0, buffer2.WrittenCount);
            Assert.AreEqual(capacity1, buffer2.Capacity);
        }
    }
}
