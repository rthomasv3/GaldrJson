namespace GaldrJson.Tests.Models
{
    [GaldrJsonSerializable]
    internal class EnumTestModel
    {
        public TestEnum TestEnumA { get; set; }
        public List<TestEnum> TestEnumList { get; set; }
        public Dictionary<TestEnum, string> TestEnumKey { get; set; }
        public Dictionary<string, TestEnum> TestEnumValue { get; set; }
    }
}
