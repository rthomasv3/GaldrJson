using GaldrJson.Tests.Models;

namespace GaldrJson.Tests;

[TestClass]
public class BasicEnumTests
{
    [TestMethod]
    public void EnumTest()
    {
        EnumTestModel enumProperties = new()
        {
            TestEnumA = TestEnum.ValueB,
            TestEnumList = new List<TestEnum>
            {
                TestEnum.ValueA,
                TestEnum.ValueC
            },
            TestEnumKey = new Dictionary<TestEnum, string>
            {
                { TestEnum.ValueA, "First" },
                { TestEnum.ValueB, "Second" }
            },
            TestEnumValue = new Dictionary<string, TestEnum>
            {
                { "Alpha", TestEnum.ValueC },
                { "Beta", TestEnum.ValueA }
            }
        };

        string json = GaldrJson.Serialize(enumProperties);
        EnumTestModel enumProperties2 = GaldrJson.Deserialize<EnumTestModel>(json);

        Assert.IsNotNull(json);
    }
}
