namespace GaldrJson.Tests.Models
{
    [GaldrJsonSerializable]
    internal class NullableTestModel
    {
        public short? TestShort { get; set; }
        public int? TestInt { get; set; }
        public long? TestLong { get; set; }
        public ushort? TestUShort { get; set; }
        public uint? TestUInt { get; set; }
        public ulong? TestULong { get; set; }
        public float? TestFloat { get; set; }
        public double? TestRealDouble { get; set; }
        public decimal? TestRealDecimal { get; set; }
        public bool? TestBool { get; set; }
        public byte? TestByte { get; set; }
        public sbyte? TestSByte { get; set; }
        public char? TestRealChar { get; set; }
        public TestEnum? TestEnum { get; set; }
    }
}
