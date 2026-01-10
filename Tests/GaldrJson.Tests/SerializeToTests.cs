using System.Text;
using System.Text.Json;
using GaldrJson.Tests.Models;

namespace GaldrJson.Tests
{
    [TestClass]
    public sealed class SerializeToTests
    {
        [TestMethod]
        public void SerializeTo_ProducesSameOutputAsSerialize()
        {
            var model = new PrimitiveTestModel
            {
                TestInt = 42,
                TestString = "Hello",
                TestBool = true,
                TestDouble = 3.14
            };

            var options = new GaldrJsonOptions { WriteIndented = false };
            string expected = GaldrJson.Serialize(model, options);

            var writer = Utf8JsonWriterCache.RentWriter(false, out var buffer);
            GaldrJson.SerializeTo(writer, model, options);
            string actual = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SerializeTo_WithIndentation()
        {
            var model = new DbCollectionTestModel
            {
                Id = 1,
                Name = "Test"
            };

            var options = new GaldrJsonOptions { WriteIndented = true };
            string expected = GaldrJson.Serialize(model, options);

            var writer = Utf8JsonWriterCache.RentWriter(true, out var buffer);
            GaldrJson.SerializeTo(writer, model, options);
            string actual = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SerializeTo_WithCustomWriter()
        {
            var model = new DbCollectionTestModel
            {
                Id = 99,
                Name = "Custom Writer Test"
            };

            var buffer = new GaldrBufferWriter();
            var writer = new Utf8JsonWriter(buffer);
            GaldrJson.SerializeTo(writer, model);
            writer.Flush();

            string json = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.Contains("99", json);
            Assert.Contains("Custom Writer Test", json);
        }

        [TestMethod]
        public void SerializeTo_NonGeneric_ProducesSameOutput()
        {
            var model = new DbCollectionTestModel
            {
                Id = 55,
                Name = "Non-Generic Test"
            };

            var options = new GaldrJsonOptions { WriteIndented = false };
            string expected = GaldrJson.Serialize(model, options);

            var buffer = new GaldrBufferWriter();
            var writer = new Utf8JsonWriter(buffer);
            GaldrJson.SerializeTo(writer, model, typeof(DbCollectionTestModel), options);
            writer.Flush();

            string actual = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.AreEqual(expected, actual);
        }
    }
}
