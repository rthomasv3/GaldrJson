using GaldrJson.Tests.Models;

namespace GaldrJson.Tests;

[TestClass]
public class NullableTests
{
    [TestMethod]
    public void TestNullable()
    {
        NullableTestModel model = new()
        {
            TestBool = true,
            TestByte = 1,
            TestEnum = TestEnum.ValueB,
            TestFloat = 1.0f,
            TestInt = 1,
            TestLong = 1L,
            TestRealChar = 'A',
            TestRealDecimal = 1.0m,
            TestRealDouble = 1.0,
            TestSByte = -1,
            TestShort = 1,
            TestUInt = 1u,
            TestULong = 1ul,
            TestUShort = 1,
        };

        string json = GaldrJson.Serialize(model);
        NullableTestModel model2 = GaldrJson.Deserialize<NullableTestModel>(json);

        Assert.IsNotNull(json);
    }

    [TestMethod]
    public void TestNullableWithNulls()
    {
        NullableTestModel model = new()
        {
            TestBool = null,
            TestByte = 1,
            TestEnum = null,
            TestFloat = 1.0f,
            TestInt = null,
            TestLong = 1L,
            TestRealChar = 'A',
            TestRealDecimal = 1.0m,
            TestRealDouble = 1.0,
            TestSByte = -1,
            TestShort = null,
            TestUInt = 1u,
            TestULong = 1ul,
            TestUShort = 1,
        };

        string json = GaldrJson.Serialize(model);
        NullableTestModel model2 = GaldrJson.Deserialize<NullableTestModel>(json);

        Assert.IsNotNull(json);
    }

    [TestMethod]
    public void TestNullableCollections()
    {
        NullableCollectionsTestModel model = new()
        {
            NullableDoubleDictionary = new Dictionary<string, double?>
            {
                { "A", 1.0 },
                { "B", null },
                { "C", 3.0 }
            },
            NullableIntList = new List<int?> { 1, null, 3 }
        };

        string json = GaldrJson.Serialize(model);
        NullableCollectionsTestModel model2 = GaldrJson.Deserialize<NullableCollectionsTestModel>(json);

        Assert.IsNotNull(json);
    }
}
