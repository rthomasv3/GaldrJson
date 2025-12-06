namespace GaldrJson.Tests.Models
{
    [GaldrJsonSerializable]
    internal class PrimitiveTestModel
    {
        public Int16 TestInt16 { get; set; }
        public short TestShort { get; set; }
        public Int32 TestInt32 { get; set; }
        public int TestInt { get; set; }
        public Int64 TestInt64 { get; set; }
        public long TestLong { get; set; }
        public UInt16 TestUInt16 { get; set; }
        public ushort TestUShort { get; set; }
        public UInt32 TestUInt32 { get; set; }
        public uint TestUInt { get; set; }
        public UInt64 TestUInt64 { get; set; }
        public ulong TestULong { get; set; }
        public Single TestSingle { get; set; }
        public float TestFloat { get; set; }
        public Double TestDouble { get; set; }
        public double TestRealDouble { get; set; }
        public Decimal TestDecimal { get; set; }
        public decimal TestRealDecimal { get; set; }
        public Boolean TestBoolean { get; set; }
        public bool TestBool { get; set; }
        public byte TestByte { get; set; }
        public sbyte TestSByte { get; set; }
        public String TestString { get; set; }
        public string TestRealString { get; set; }
        public Char TestChar { get; set; }
        public char TestRealChar { get; set; }
    }
}
