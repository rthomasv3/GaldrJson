using GaldrJson.Tests.Models;

namespace GaldrJson.Tests
{
    [TestClass]
    public sealed class PrimitiveTests
    {
        [TestMethod]
        public void TestPrimitivesSerialize()
        {
            PrimitiveTestModel primitiveProperties = new()
            {
                TestBool = true,
                TestBoolean = false,
                TestByte = 123,
                TestChar = 'A',
                TestDecimal = 123.45M,
                TestDouble = 123.456,
                TestFloat = 12.34F,
                TestInt = 123456,
                TestInt16 = 12345,
                TestInt32 = 234567,
                TestInt64 = 3456789,
                TestLong = 4567890,
                TestRealChar = 'Z',
                TestRealDecimal = 678.90M,
                TestRealDouble = 654.321,
                TestRealString = "Goodbye, World!",
                TestSByte = 100,
                TestShort = 23456,
                TestSingle = 43.21F,
                TestString = "Hello, World!",
                TestUInt = 345678U,
                TestUInt16 = 54321,
                TestUInt32 = 456789U,
                TestUInt64 = 5678901UL,
                TestULong = 6789012UL,
                TestUShort = 54321,
            };

            string testJson = GaldrJson.Serialize(primitiveProperties);
            PrimitiveTestModel primitiveProperties2 = GaldrJson.Deserialize<PrimitiveTestModel>(testJson);

            Assert.IsNotNull(testJson);
        }

        [TestMethod]
        public void TestPrimitivesDeserialize()
        {
            string json = """
            {
              "testInt16": 12345,
              "testShort": 23456,
              "testInt32": 234567,
              "testInt": 123456,
              "testInt64": 3456789,
              "testLong": 4567890,
              "testUInt16": 54321,
              "testUShort": 54321,
              "testUInt32": 456789,
              "testUInt": 345678,
              "testUInt64": 5678901,
              "testULong": 6789012,
              "testSingle": 43.21,
              "testFloat": 12.34,
              "testDouble": 123.456,
              "testRealDouble": 654.321,
              "testDecimal": 123.45,
              "testRealDecimal": 678.90,
              "testBoolean": false,
              "testBool": true,
              "testByte": 123,
              "testSByte": 100,
              "testString": "Hello, World!",
              "testRealString": "Goodbye, World!",
              "testChar": "A",
              "testRealChar": "Z"
            }
            """;

            PrimitiveTestModel primitiveProperties = GaldrJson.Deserialize<PrimitiveTestModel>(json);

            Assert.IsNotNull(primitiveProperties);
        }
    }
}
