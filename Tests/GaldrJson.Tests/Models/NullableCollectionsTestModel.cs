namespace GaldrJson.Tests.Models
{
    [GaldrJsonSerializable]
    internal class NullableCollectionsTestModel
    {
        public List<int?> NullableIntList { get; set; }
        public Dictionary<string, double?> NullableDoubleDictionary { get; set; }
    }
}
