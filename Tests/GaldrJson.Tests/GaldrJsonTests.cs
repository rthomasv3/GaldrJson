using System.Text.Json;

namespace GaldrJson.Tests
{
    // ============================================================================
    // TEST MODELS
    // ============================================================================

    #region Basic Types Test Models

    [GaldrJsonSerializable]
    public class PrimitiveTypesModel
    {
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public short ShortValue { get; set; }
        public ushort UShortValue { get; set; }
        public int IntValue { get; set; }
        public uint UIntValue { get; set; }
        public long LongValue { get; set; }
        public ulong ULongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public bool BoolValue { get; set; }
        public char CharValue { get; set; }
        public string StringValue { get; set; }
    }

    [GaldrJsonSerializable]
    public class DateAndTimeModel
    {
        public DateTime DateTime { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public Guid Guid { get; set; }
    }

    #endregion

    #region Nullable Types Test Models

    [GaldrJsonSerializable]
    public class NullablePrimitivesModel
    {
        public int? NullableInt { get; set; }
        public long? NullableLong { get; set; }
        public double? NullableDouble { get; set; }
        public bool? NullableBool { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public TimeSpan? NullableTimeSpan { get; set; }
        public Guid? NullableGuid { get; set; }
        public char? NullableChar { get; set; }
        public decimal? NullableDecimal { get; set; }
    }

    #endregion

    #region Enum Test Models

    public enum StatusEnum
    {
        Inactive = 0,
        Active = 1,
        Pending = 2
    }

    public enum PriorityEnum
    {
        Low = 1,
        Medium = 5,
        High = 10
    }

    [GaldrJsonSerializable]
    public class EnumModel
    {
        public StatusEnum Status { get; set; }
        public PriorityEnum Priority { get; set; }
        public StatusEnum? NullableStatus { get; set; }
    }

    #endregion

    #region Collection Test Models

    [GaldrJsonSerializable]
    public class CollectionModel
    {
        public List<int> IntList { get; set; }
        public List<string> StringList { get; set; }
        public int[] IntArray { get; set; }
        public string[] StringArray { get; set; }
        public List<double> DoubleList { get; set; }
        public List<bool> BoolList { get; set; }
    }

    [GaldrJsonSerializable]
    public class DateCollectionModel
    {
        public List<DateTime> DateTimeList { get; set; }
        public List<TimeSpan> TimeSpanList { get; set; }
        public List<Guid> GuidList { get; set; }
        public DateTime[] DateTimeArray { get; set; }
    }

    [GaldrJsonSerializable]
    public class EnumCollectionModel
    {
        public List<StatusEnum> StatusList { get; set; }
        public StatusEnum[] StatusArray { get; set; }
        public List<StatusEnum?> NullableStatusList { get; set; }
    }

    [GaldrJsonSerializable]
    public class NullableCollectionModel
    {
        public List<int?> NullableIntList { get; set; }
        public List<string> NullableStringList { get; set; }
        public List<double?> NullableDoubleList { get; set; }
        public List<DateTime?> NullableDateTimeList { get; set; }
    }

    [GaldrJsonSerializable]
    public class DataModel
    {
        public byte[] ByteArray { get; set; }
        public List<byte> ByteList { get; set; }
    }

    #endregion

    #region Dictionary Test Models

    [GaldrJsonSerializable]
    public class DictionaryModel
    {
        public Dictionary<string, int> StringIntDict { get; set; }
        public Dictionary<int, string> IntStringDict { get; set; }
        public Dictionary<string, string> StringStringDict { get; set; }
        public Dictionary<Guid, string> GuidStringDict { get; set; }
    }

    [GaldrJsonSerializable]
    public class DictionaryComplexValueModel
    {
        public Dictionary<string, DateTime> DateTimeDict { get; set; }
        public Dictionary<string, List<int>> ListDict { get; set; }
        public Dictionary<int, StatusEnum> EnumDict { get; set; }
        public Dictionary<string, Address> AddressDict { get; set; }
    }

    [GaldrJsonSerializable]
    public class DictionaryNullableModel
    {
        public Dictionary<string, int?> NullableValueDict { get; set; }
        public Dictionary<int, double?> NullableDoubleDict { get; set; }
    }

    #endregion

    #region Nested Object Test Models

    [GaldrJsonSerializable]
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
    }

    [GaldrJsonSerializable]
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }
    }

    [GaldrJsonSerializable]
    public class Company
    {
        public string Name { get; set; }
        public List<Person> Employees { get; set; }
        public Address HeadquartersAddress { get; set; }
    }

    #endregion

    #region Property Name Test Models

    // Note: These would require GaldrJsonPropertyName attribute if implemented
    [GaldrJsonSerializable]
    public class CamelCaseModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int PersonAge { get; set; }
    }

    #endregion

    #region Null Handling Test Models

    [GaldrJsonSerializable]
    public class NullableReferenceModel
    {
        public string NullableString { get; set; }
        public Address NullableAddress { get; set; }
        public List<int> NullableList { get; set; }
        public Dictionary<string, int> NullableDict { get; set; }
    }

    #endregion

    #region Edge Case Test Models

    [GaldrJsonSerializable]
    public class EmptyModel
    {
        // No properties - edge case
    }

    [GaldrJsonSerializable]
    public class SinglePropertyModel
    {
        public int Value { get; set; }
    }

    [GaldrJsonSerializable]
    public class DeepNestingModel
    {
        public NestedLevel1 Level1 { get; set; }
    }

    [GaldrJsonSerializable]
    public class NestedLevel1
    {
        public string Data1 { get; set; }
        public NestedLevel2 Level2 { get; set; }
    }

    [GaldrJsonSerializable]
    public class NestedLevel2
    {
        public string Data2 { get; set; }
        public NestedLevel3 Level3 { get; set; }
    }

    [GaldrJsonSerializable]
    public class NestedLevel3
    {
        public string Data3 { get; set; }
        public int FinalValue { get; set; }
    }

    #endregion

    #region Init Test Models

    [GaldrJsonSerializable]
    internal class InitPropertyTestModel
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public double Price { get; init; }
    }

    [GaldrJsonSerializable]
    internal class MixedPropertyTestModel
    {
        public int Id { get; init; }       // init
        public string Name { get; set; }   // set
        public double Price { get; init; } // init
        public bool IsActive { get; set; } // set
    }

    [GaldrJsonSerializable]
    internal class ComplexInitPropertyTestModel
    {
        public int Id { get; init; }
        public List<int> Numbers { get; init; }
        public Dictionary<string, string> Tags { get; init; }
        public InitPropertyTestModel Nested { get; init; }
    }

    [GaldrJsonSerializable]
    internal class NullableInitPropertyTestModel
    {
        public int? NullableId { get; init; }
        public string NullableString { get; init; }
        public DateTime? NullableDate { get; init; }
    }

    #endregion

    #region Circular Reference Tests

    [GaldrJsonSerializable]
    public class CircularPerson
    {
        public string Name { get; set; }
        public CircularAddress Address { get; set; }
    }

    [GaldrJsonSerializable]
    public class CircularAddress
    {
        public string Street { get; set; }
        public CircularPerson Person { get; set; }
    }

    #endregion

    // ============================================================================
    // TESTS
    // ============================================================================

    [TestClass]
    public class PrimitiveTypesTests
    {
        [TestMethod]
        public void TestAllPrimitiveTypes_Serialize()
        {
            var model = new PrimitiveTypesModel
            {
                ByteValue = 255,
                SByteValue = -128,
                ShortValue = -32768,
                UShortValue = 65535,
                IntValue = -2147483648,
                UIntValue = 4294967295,
                LongValue = -9223372036854775808,
                ULongValue = 18446744073709551615,
                FloatValue = 3.14159f,
                DoubleValue = 2.71828,
                DecimalValue = 123.456m,
                BoolValue = true,
                CharValue = 'A',
                StringValue = "Hello, World!"
            };

            string json = GaldrJson.Serialize(model);

            Assert.IsNotNull(json);
            Assert.Contains("255", json);
            Assert.Contains("-128", json);
            Assert.Contains("Hello, World!", json);
        }

        [TestMethod]
        public void TestAllPrimitiveTypes_RoundTrip()
        {
            var original = new PrimitiveTypesModel
            {
                ByteValue = 100,
                SByteValue = -50,
                ShortValue = -1000,
                UShortValue = 2000,
                IntValue = -100000,
                UIntValue = 200000,
                LongValue = -1000000000,
                ULongValue = 2000000000,
                FloatValue = 1.23f,
                DoubleValue = 4.56,
                DecimalValue = 789.012m,
                BoolValue = false,
                CharValue = 'Z',
                StringValue = "Test String"
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<PrimitiveTypesModel>(json);

            Assert.AreEqual(original.ByteValue, deserialized.ByteValue);
            Assert.AreEqual(original.SByteValue, deserialized.SByteValue);
            Assert.AreEqual(original.ShortValue, deserialized.ShortValue);
            Assert.AreEqual(original.UShortValue, deserialized.UShortValue);
            Assert.AreEqual(original.IntValue, deserialized.IntValue);
            Assert.AreEqual(original.UIntValue, deserialized.UIntValue);
            Assert.AreEqual(original.LongValue, deserialized.LongValue);
            Assert.AreEqual(original.ULongValue, deserialized.ULongValue);
            Assert.AreEqual(original.FloatValue, deserialized.FloatValue, 0.0001f);
            Assert.AreEqual(original.DoubleValue, deserialized.DoubleValue, 0.0001);
            Assert.AreEqual(original.DecimalValue, deserialized.DecimalValue);
            Assert.AreEqual(original.BoolValue, deserialized.BoolValue);
            Assert.AreEqual(original.CharValue, deserialized.CharValue);
            Assert.AreEqual(original.StringValue, deserialized.StringValue);
        }

        [TestMethod]
        public void TestDateAndTime_RoundTrip()
        {
            var original = new DateAndTimeModel
            {
                DateTime = new DateTime(2024, 6, 20, 14, 30, 0, DateTimeKind.Utc),
                DateTimeOffset = new DateTimeOffset(2024, 6, 20, 14, 30, 0, TimeSpan.FromHours(-5)),
                TimeSpan = TimeSpan.FromHours(5.5),
                Guid = Guid.NewGuid()
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DateAndTimeModel>(json);

            Assert.AreEqual(original.DateTime, deserialized.DateTime);
            Assert.AreEqual(original.DateTimeOffset, deserialized.DateTimeOffset);
            Assert.AreEqual(original.TimeSpan, deserialized.TimeSpan);
            Assert.AreEqual(original.Guid, deserialized.Guid);
        }
    }

    [TestClass]
    public class NullableTypesTests
    {
        [TestMethod]
        public void TestNullablePrimitives_WithValues_RoundTrip()
        {
            var original = new NullablePrimitivesModel
            {
                NullableInt = 42,
                NullableLong = 123456789L,
                NullableDouble = 3.14159,
                NullableBool = true,
                NullableDateTime = new DateTime(2024, 1, 1),
                NullableTimeSpan = TimeSpan.FromMinutes(30),
                NullableGuid = Guid.NewGuid(),
                NullableChar = 'X',
                NullableDecimal = 99.99m
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullablePrimitivesModel>(json);

            Assert.AreEqual(original.NullableInt, deserialized.NullableInt);
            Assert.AreEqual(original.NullableLong, deserialized.NullableLong);
            Assert.AreEqual(original.NullableDouble, deserialized.NullableDouble);
            Assert.AreEqual(original.NullableBool, deserialized.NullableBool);
            Assert.AreEqual(original.NullableDateTime, deserialized.NullableDateTime);
            Assert.AreEqual(original.NullableTimeSpan, deserialized.NullableTimeSpan);
            Assert.AreEqual(original.NullableGuid, deserialized.NullableGuid);
            Assert.AreEqual(original.NullableChar, deserialized.NullableChar);
            Assert.AreEqual(original.NullableDecimal, deserialized.NullableDecimal);
        }

        [TestMethod]
        public void TestNullablePrimitives_WithNulls_RoundTrip()
        {
            var original = new NullablePrimitivesModel
            {
                NullableInt = null,
                NullableLong = null,
                NullableDouble = null,
                NullableBool = null,
                NullableDateTime = null,
                NullableTimeSpan = null,
                NullableGuid = null,
                NullableChar = null,
                NullableDecimal = null
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullablePrimitivesModel>(json);

            Assert.IsNull(deserialized.NullableInt);
            Assert.IsNull(deserialized.NullableLong);
            Assert.IsNull(deserialized.NullableDouble);
            Assert.IsNull(deserialized.NullableBool);
            Assert.IsNull(deserialized.NullableDateTime);
            Assert.IsNull(deserialized.NullableTimeSpan);
            Assert.IsNull(deserialized.NullableGuid);
            Assert.IsNull(deserialized.NullableChar);
            Assert.IsNull(deserialized.NullableDecimal);
        }

        [TestMethod]
        public void TestNullablePrimitives_MixedNullsAndValues_RoundTrip()
        {
            var original = new NullablePrimitivesModel
            {
                NullableInt = 42,
                NullableLong = null,
                NullableDouble = 3.14,
                NullableBool = null,
                NullableDateTime = new DateTime(2024, 6, 20),
                NullableTimeSpan = null,
                NullableGuid = Guid.NewGuid(),
                NullableChar = null,
                NullableDecimal = 123.45m
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullablePrimitivesModel>(json);

            Assert.AreEqual(original.NullableInt, deserialized.NullableInt);
            Assert.IsNull(deserialized.NullableLong);
            Assert.AreEqual(original.NullableDouble, deserialized.NullableDouble);
            Assert.IsNull(deserialized.NullableBool);
            Assert.AreEqual(original.NullableDateTime, deserialized.NullableDateTime);
            Assert.IsNull(deserialized.NullableTimeSpan);
            Assert.AreEqual(original.NullableGuid, deserialized.NullableGuid);
            Assert.IsNull(deserialized.NullableChar);
            Assert.AreEqual(original.NullableDecimal, deserialized.NullableDecimal);
        }
    }

    [TestClass]
    public class EnumTests
    {
        [TestMethod]
        public void TestEnums_RoundTrip()
        {
            var original = new EnumModel
            {
                Status = StatusEnum.Active,
                Priority = PriorityEnum.High,
                NullableStatus = StatusEnum.Pending
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<EnumModel>(json);

            Assert.AreEqual(original.Status, deserialized.Status);
            Assert.AreEqual(original.Priority, deserialized.Priority);
            Assert.AreEqual(original.NullableStatus, deserialized.NullableStatus);
        }

        [TestMethod]
        public void TestEnums_NullableNull_RoundTrip()
        {
            var original = new EnumModel
            {
                Status = StatusEnum.Inactive,
                Priority = PriorityEnum.Low,
                NullableStatus = null
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<EnumModel>(json);

            Assert.AreEqual(original.Status, deserialized.Status);
            Assert.AreEqual(original.Priority, deserialized.Priority);
            Assert.IsNull(deserialized.NullableStatus);
        }

        [TestMethod]
        public void TestEnums_SerializesAsInteger()
        {
            var model = new EnumModel
            {
                Status = StatusEnum.Active,
                Priority = PriorityEnum.Medium
            };

            string json = GaldrJson.Serialize(model);

            // Enums should be serialized as integers
            Assert.Contains("1", json); // Active = 1
            Assert.Contains("5", json); // Medium = 5
        }
    }

    [TestClass]
    public class CollectionTests
    {
        [TestMethod]
        public void TestPrimitiveCollections_Lists_RoundTrip()
        {
            var original = new CollectionModel
            {
                IntList = new List<int> { 1, 2, 3, 4, 5 },
                StringList = new List<string> { "apple", "banana", "cherry" },
                DoubleList = new List<double> { 1.1, 2.2, 3.3 },
                BoolList = new List<bool> { true, false, true }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<CollectionModel>(json);

            CollectionAssert.AreEqual(original.IntList, deserialized.IntList);
            CollectionAssert.AreEqual(original.StringList, deserialized.StringList);
            CollectionAssert.AreEqual(original.DoubleList, deserialized.DoubleList);
            CollectionAssert.AreEqual(original.BoolList, deserialized.BoolList);
        }

        [TestMethod]
        public void TestPrimitiveCollections_Arrays_RoundTrip()
        {
            var original = new CollectionModel
            {
                IntArray = new int[] { 10, 20, 30 },
                StringArray = new string[] { "one", "two", "three" }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<CollectionModel>(json);

            CollectionAssert.AreEqual(original.IntArray, deserialized.IntArray);
            CollectionAssert.AreEqual(original.StringArray, deserialized.StringArray);
        }

        [TestMethod]
        public void TestPrimitiveCollections_EmptyLists_RoundTrip()
        {
            var original = new CollectionModel
            {
                IntList = new List<int>(),
                StringList = new List<string>(),
                IntArray = new int[0],
                StringArray = new string[0]
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<CollectionModel>(json);

            Assert.IsEmpty(deserialized.IntList);
            Assert.IsEmpty(deserialized.StringList);
            Assert.IsEmpty(deserialized.IntArray);
            Assert.IsEmpty(deserialized.StringArray);
        }

        [TestMethod]
        public void TestDateCollections_RoundTrip()
        {
            var original = new DateCollectionModel
            {
                DateTimeList = new List<DateTime>
                {
                    new DateTime(2024, 1, 1),
                    new DateTime(2024, 12, 31)
                },
                TimeSpanList = new List<TimeSpan>
                {
                    TimeSpan.FromHours(1),
                    TimeSpan.FromMinutes(30)
                },
                GuidList = new List<Guid>
                {
                    Guid.NewGuid(),
                    Guid.NewGuid()
                },
                DateTimeArray = new DateTime[]
                {
                    new DateTime(2024, 6, 20)
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DateCollectionModel>(json);

            CollectionAssert.AreEqual(original.DateTimeList, deserialized.DateTimeList);
            CollectionAssert.AreEqual(original.TimeSpanList, deserialized.TimeSpanList);
            CollectionAssert.AreEqual(original.GuidList, deserialized.GuidList);
            CollectionAssert.AreEqual(original.DateTimeArray, deserialized.DateTimeArray);
        }

        [TestMethod]
        public void TestEnumCollections_RoundTrip()
        {
            var original = new EnumCollectionModel
            {
                StatusList = new List<StatusEnum> { StatusEnum.Active, StatusEnum.Pending, StatusEnum.Inactive },
                StatusArray = new StatusEnum[] { StatusEnum.Pending, StatusEnum.Active },
                NullableStatusList = new List<StatusEnum?> { StatusEnum.Active, null, StatusEnum.Pending }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<EnumCollectionModel>(json);

            CollectionAssert.AreEqual(original.StatusList, deserialized.StatusList);
            CollectionAssert.AreEqual(original.StatusArray, deserialized.StatusArray);
            CollectionAssert.AreEqual(original.NullableStatusList, deserialized.NullableStatusList);
        }

        [TestMethod]
        public void TestNullableCollections_RoundTrip()
        {
            var original = new NullableCollectionModel
            {
                NullableIntList = new List<int?> { 1, null, 3, null, 5 },
                NullableDoubleList = new List<double?> { 1.1, null, 3.3 },
                NullableDateTimeList = new List<DateTime?> { new DateTime(2024, 1, 1), null, new DateTime(2024, 12, 31) },
                NullableStringList = new List<string> { "hello", null, "world" }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullableCollectionModel>(json);

            CollectionAssert.AreEqual(original.NullableIntList, deserialized.NullableIntList);
            CollectionAssert.AreEqual(original.NullableDoubleList, deserialized.NullableDoubleList);
            CollectionAssert.AreEqual(original.NullableDateTimeList, deserialized.NullableDateTimeList);
            CollectionAssert.AreEqual(original.NullableStringList, deserialized.NullableStringList);
        }

        [TestMethod]
        public void TestByteCollection()
        {
            var original = new DataModel()
            {
                ByteArray = [72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100],
                ByteList = [72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100],
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DataModel>(json);

            // Verify it's Base64 encoded in JSON
            Assert.Contains("\"SGVsbG8gV29ybGQ=\"", json);

            // Verify round-trip works correctly
            CollectionAssert.AreEqual(original.ByteArray, deserialized.ByteArray);
            CollectionAssert.AreEqual(original.ByteList, deserialized.ByteList);
        }

        [TestMethod]
        public void TestByteCollection_WithNulls()
        {
            var original = new DataModel()
            {
                ByteArray = null,
                ByteList = null,
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DataModel>(json);

            Assert.IsNull(deserialized.ByteArray);
            Assert.IsNull(deserialized.ByteList);
        }

        [TestMethod]
        public void TestByteCollection_Empty()
        {
            var original = new DataModel()
            {
                ByteArray = [],
                ByteList = [],
            };

            string json = GaldrJson.Serialize(original, new GaldrJsonOptions()
            {
                PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var deserialized = GaldrJson.Deserialize<DataModel>(json);

            Assert.IsNotNull(deserialized.ByteArray);
            Assert.IsNotNull(deserialized.ByteList);
            Assert.IsEmpty(deserialized.ByteArray);
            Assert.IsEmpty(deserialized.ByteList);

            // Empty byte arrays should serialize as empty Base64 string
            Assert.Contains("byteArray\":\"\"", json);
            Assert.Contains("byteList\":\"\"", json);
        }

        [TestMethod]
        public void TestByteCollection_LargeData()
        {
            var largeData = new byte[1000];
            for (int i = 0; i < 1000; i++)
            {
                largeData[i] = (byte)(i % 256);
            }

            var original = new DataModel()
            {
                ByteArray = largeData,
                ByteList = new List<byte>(largeData),
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DataModel>(json);

            CollectionAssert.AreEqual(original.ByteArray, deserialized.ByteArray);
            CollectionAssert.AreEqual(original.ByteList, deserialized.ByteList);
        }
    }

    [TestClass]
    public class DictionaryTests
    {
        [TestMethod]
        public void TestBasicDictionaries_RoundTrip()
        {
            var original = new DictionaryModel
            {
                StringIntDict = new Dictionary<string, int>
                {
                    { "one", 1 },
                    { "two", 2 },
                    { "three", 3 }
                },
                IntStringDict = new Dictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                },
                StringStringDict = new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                },
                GuidStringDict = new Dictionary<Guid, string>()
            };

            var guid = Guid.NewGuid();
            original.GuidStringDict[guid] = "test";

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DictionaryModel>(json);

            CollectionAssert.AreEqual(original.StringIntDict, deserialized.StringIntDict);
            CollectionAssert.AreEqual(original.IntStringDict, deserialized.IntStringDict);
            CollectionAssert.AreEqual(original.StringStringDict, deserialized.StringStringDict);
            CollectionAssert.AreEqual(original.GuidStringDict, deserialized.GuidStringDict);
        }

        [TestMethod]
        public void TestComplexValueDictionaries_RoundTrip()
        {
            var original = new DictionaryComplexValueModel
            {
                DateTimeDict = new Dictionary<string, DateTime>
                {
                    { "start", new DateTime(2024, 1, 1) },
                    { "end", new DateTime(2024, 12, 31) }
                },
                ListDict = new Dictionary<string, List<int>>
                {
                    { "evens", new List<int> { 2, 4, 6 } },
                    { "odds", new List<int> { 1, 3, 5 } }
                },
                EnumDict = new Dictionary<int, StatusEnum>
                {
                    { 1, StatusEnum.Active },
                    { 2, StatusEnum.Pending }
                },
                AddressDict = new Dictionary<string, Address>
                {
                    { "home", new Address() { City = "Somewhere" } },
                },
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DictionaryComplexValueModel>(json);

            CollectionAssert.AreEqual(original.DateTimeDict, deserialized.DateTimeDict);
            Assert.HasCount(original.ListDict.Count, deserialized.ListDict);
            CollectionAssert.AreEqual(original.ListDict["evens"], deserialized.ListDict["evens"]);
            CollectionAssert.AreEqual(original.EnumDict, deserialized.EnumDict);
            Assert.HasCount(original.AddressDict.Count, deserialized.AddressDict);
        }

        [TestMethod]
        public void TestNullableValueDictionaries_RoundTrip()
        {
            var original = new DictionaryNullableModel
            {
                NullableValueDict = new Dictionary<string, int?>
                {
                    { "one", 1 },
                    { "null", null },
                    { "three", 3 }
                },
                NullableDoubleDict = new Dictionary<int, double?>
                {
                    { 1, 1.1 },
                    { 2, null }
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DictionaryNullableModel>(json);

            CollectionAssert.AreEqual(original.NullableValueDict, deserialized.NullableValueDict);
            CollectionAssert.AreEqual(original.NullableDoubleDict, deserialized.NullableDoubleDict);
        }

        [TestMethod]
        public void TestEmptyDictionaries_RoundTrip()
        {
            var original = new DictionaryModel
            {
                StringIntDict = new Dictionary<string, int>(),
                IntStringDict = new Dictionary<int, string>(),
                StringStringDict = new Dictionary<string, string>(),
                GuidStringDict = new Dictionary<Guid, string>()
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DictionaryModel>(json);

            Assert.IsEmpty(deserialized.StringIntDict);
            Assert.IsEmpty(deserialized.IntStringDict);
            Assert.IsEmpty(deserialized.StringStringDict);
            Assert.IsEmpty(deserialized.GuidStringDict);
        }
    }

    [TestClass]
    public class NestedObjectTests
    {
        [TestMethod]
        public void TestNaming()
        {
            var original = new Address
            {
                Street = "123 Main St",
                City = "Anytown",
                ZipCode = "12345"
            };

            string json = GaldrJson.Serialize(original, new GaldrJsonOptions()
            {
                PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
            });

            Assert.Contains("\"street\"", json);
            Assert.Contains("\"city\"", json);
            Assert.Contains("\"zipCode\"", json);
        }

        [TestMethod]
        public void TestSimpleNesting_RoundTrip()
        {
            var original = new Person
            {
                Name = "John Doe",
                Age = 30,
                Address = new Address
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    ZipCode = "12345"
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<Person>(json);

            Assert.AreEqual(original.Name, deserialized.Name);
            Assert.AreEqual(original.Age, deserialized.Age);
            Assert.IsNotNull(deserialized.Address);
            Assert.AreEqual(original.Address.Street, deserialized.Address.Street);
            Assert.AreEqual(original.Address.City, deserialized.Address.City);
            Assert.AreEqual(original.Address.ZipCode, deserialized.Address.ZipCode);
        }

        [TestMethod]
        public void TestComplexNesting_RoundTrip()
        {
            var original = new Company
            {
                Name = "Acme Corp",
                HeadquartersAddress = new Address
                {
                    Street = "456 Business Ave",
                    City = "Corporate City",
                    ZipCode = "54321"
                },
                Employees = new List<Person>
                {
                    new Person
                    {
                        Name = "Alice",
                        Age = 25,
                        Address = new Address { Street = "1 First St", City = "Town A", ZipCode = "11111" }
                    },
                    new Person
                    {
                        Name = "Bob",
                        Age = 35,
                        Address = new Address { Street = "2 Second St", City = "Town B", ZipCode = "22222" }
                    }
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<Company>(json);

            Assert.AreEqual(original.Name, deserialized.Name);
            Assert.IsNotNull(deserialized.HeadquartersAddress);
            Assert.AreEqual(original.HeadquartersAddress.City, deserialized.HeadquartersAddress.City);
            Assert.HasCount(2, deserialized.Employees);
            Assert.AreEqual(original.Employees[0].Name, deserialized.Employees[0].Name);
            Assert.AreEqual(original.Employees[1].Address.ZipCode, deserialized.Employees[1].Address.ZipCode);
        }

        [TestMethod]
        public void TestDeepNesting_RoundTrip()
        {
            var original = new DeepNestingModel
            {
                Level1 = new NestedLevel1
                {
                    Data1 = "Level 1",
                    Level2 = new NestedLevel2
                    {
                        Data2 = "Level 2",
                        Level3 = new NestedLevel3
                        {
                            Data3 = "Level 3",
                            FinalValue = 42
                        }
                    }
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<DeepNestingModel>(json);

            Assert.AreEqual(original.Level1.Data1, deserialized.Level1.Data1);
            Assert.AreEqual(original.Level1.Level2.Data2, deserialized.Level1.Level2.Data2);
            Assert.AreEqual(original.Level1.Level2.Level3.Data3, deserialized.Level1.Level2.Level3.Data3);
            Assert.AreEqual(original.Level1.Level2.Level3.FinalValue, deserialized.Level1.Level2.Level3.FinalValue);
        }
    }

    [TestClass]
    public class NullHandlingTests
    {
        [TestMethod]
        public void TestNullReferenceTypes_RoundTrip()
        {
            var original = new NullableReferenceModel
            {
                NullableString = null,
                NullableAddress = null,
                NullableList = null,
                NullableDict = null
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullableReferenceModel>(json);

            Assert.IsNull(deserialized.NullableString);
            Assert.IsNull(deserialized.NullableAddress);
            Assert.IsNull(deserialized.NullableList);
            Assert.IsNull(deserialized.NullableDict);
        }

        [TestMethod]
        public void TestNullNestedObject_RoundTrip()
        {
            var original = new Person
            {
                Name = "Jane Doe",
                Age = 28,
                Address = null
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<Person>(json);

            Assert.AreEqual(original.Name, deserialized.Name);
            Assert.AreEqual(original.Age, deserialized.Age);
            Assert.IsNull(deserialized.Address);
        }
    }

    [TestClass]
    public class PropertyNamingTests
    {
        [TestMethod]
        public void TestCamelCaseNaming()
        {
            var model = new CamelCaseModel
            {
                FirstName = "John",
                LastName = "Doe",
                PersonAge = 30
            };

            string json = GaldrJson.Serialize(model);

            // Properties should be camelCase in JSON by default
            Assert.IsTrue(json.Contains("firstName") || json.Contains("FirstName"));
            Assert.IsTrue(json.Contains("lastName") || json.Contains("LastName"));
            Assert.IsTrue(json.Contains("personAge") || json.Contains("PersonAge"));
        }
    }

    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void TestEmptyModel_RoundTrip()
        {
            var original = new EmptyModel();

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<EmptyModel>(json);

            Assert.IsNotNull(deserialized);
        }

        [TestMethod]
        public void TestSingleProperty_RoundTrip()
        {
            var original = new SinglePropertyModel { Value = 123 };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<SinglePropertyModel>(json);

            Assert.AreEqual(original.Value, deserialized.Value);
        }

        [TestMethod]
        public void TestStringWithSpecialCharacters_RoundTrip()
        {
            var original = new PrimitiveTypesModel
            {
                StringValue = "Line1\nLine2\tTabbed\"Quoted\""
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<PrimitiveTypesModel>(json);

            Assert.AreEqual(original.StringValue, deserialized.StringValue);
        }

        [TestMethod]
        public void TestEmptyString_RoundTrip()
        {
            var original = new PrimitiveTypesModel
            {
                StringValue = ""
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<PrimitiveTypesModel>(json);

            Assert.AreEqual(original.StringValue, deserialized.StringValue);
        }

        [TestMethod]
        public void TestMinMaxValues_RoundTrip()
        {
            var original = new PrimitiveTypesModel
            {
                ByteValue = byte.MaxValue,
                SByteValue = sbyte.MinValue,
                ShortValue = short.MinValue,
                UShortValue = ushort.MaxValue,
                IntValue = int.MinValue,
                UIntValue = uint.MaxValue,
                LongValue = long.MinValue,
                ULongValue = ulong.MaxValue,
                DecimalValue = decimal.MaxValue
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<PrimitiveTypesModel>(json);

            Assert.AreEqual(original.ByteValue, deserialized.ByteValue);
            Assert.AreEqual(original.SByteValue, deserialized.SByteValue);
            Assert.AreEqual(original.ShortValue, deserialized.ShortValue);
            Assert.AreEqual(original.UShortValue, deserialized.UShortValue);
            Assert.AreEqual(original.IntValue, deserialized.IntValue);
            Assert.AreEqual(original.UIntValue, deserialized.UIntValue);
            Assert.AreEqual(original.LongValue, deserialized.LongValue);
            Assert.AreEqual(original.ULongValue, deserialized.ULongValue);
            Assert.AreEqual(original.DecimalValue, deserialized.DecimalValue);
        }
    }

    [TestClass]
    public class ErrorHandlingTests
    {
        [TestMethod]
        //[ExpectedException(typeof(NotSupportedException))]
        public void TestSerializeUnregisteredType_ThrowsException()
        {
            var unregisteredObject = new { Name = "Test" };
            Assert.Throws<NotSupportedException>(() => GaldrJson.Serialize(unregisteredObject));
        }

        [TestMethod]
        //[ExpectedException(typeof(NotSupportedException))]
        public void TestDeserializeUnregisteredType_ThrowsException()
        {
            string json = "{\"name\":\"Test\"}";
            Assert.Throws<NotSupportedException>(() => GaldrJson.Deserialize<object>(json));
        }
    }

    [TestClass]
    public class InitPropertyTests
    {
        [TestMethod]
        public void TestInitProperties_RoundTrip()
        {
            var original = new InitPropertyTestModel
            {
                Id = 42,
                Name = "Test Product",
                Price = 99.99
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.AreEqual(original.Id, deserialized.Id);
            Assert.AreEqual(original.Name, deserialized.Name);
            Assert.AreEqual(original.Price, deserialized.Price);
        }

        [TestMethod]
        public void TestInitProperties_Deserialize()
        {
            string json = @"{
                ""id"": 123,
                ""name"": ""Widget"",
                ""price"": 49.95
            }";

            var result = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.AreEqual(123, result.Id);
            Assert.AreEqual("Widget", result.Name);
            Assert.AreEqual(49.95, result.Price);
        }

        [TestMethod]
        public void TestMixedProperties_InitAndSet_RoundTrip()
        {
            var original = new MixedPropertyTestModel
            {
                Id = 1,
                Name = "Mixed",
                Price = 25.50,
                IsActive = true
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<MixedPropertyTestModel>(json);

            Assert.AreEqual(original.Id, deserialized.Id);
            Assert.AreEqual(original.Name, deserialized.Name);
            Assert.AreEqual(original.Price, deserialized.Price);
            Assert.AreEqual(original.IsActive, deserialized.IsActive);
        }

        [TestMethod]
        public void TestComplexInitProperties_RoundTrip()
        {
            var original = new ComplexInitPropertyTestModel
            {
                Id = 999,
                Numbers = new List<int> { 1, 2, 3, 4, 5 },
                Tags = new Dictionary<string, string>
                {
                    { "category", "electronics" },
                    { "brand", "acme" }
                },
                Nested = new InitPropertyTestModel
                {
                    Id = 111,
                    Name = "Nested Item",
                    Price = 12.34
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<ComplexInitPropertyTestModel>(json);

            Assert.AreEqual(original.Id, deserialized.Id);
            CollectionAssert.AreEqual(original.Numbers, deserialized.Numbers);
            CollectionAssert.AreEqual(original.Tags, deserialized.Tags);
            Assert.IsNotNull(deserialized.Nested);
            Assert.AreEqual(original.Nested.Id, deserialized.Nested.Id);
            Assert.AreEqual(original.Nested.Name, deserialized.Nested.Name);
            Assert.AreEqual(original.Nested.Price, deserialized.Nested.Price);
        }

        [TestMethod]
        public void TestNullableInitProperties_WithValues_RoundTrip()
        {
            var original = new NullableInitPropertyTestModel
            {
                NullableId = 42,
                NullableString = "Hello",
                NullableDate = new DateTime(2024, 6, 20)
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullableInitPropertyTestModel>(json);

            Assert.AreEqual(original.NullableId, deserialized.NullableId);
            Assert.AreEqual(original.NullableString, deserialized.NullableString);
            Assert.AreEqual(original.NullableDate, deserialized.NullableDate);
        }

        [TestMethod]
        public void TestNullableInitProperties_WithNulls_RoundTrip()
        {
            var original = new NullableInitPropertyTestModel
            {
                NullableId = null,
                NullableString = null,
                NullableDate = null
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<NullableInitPropertyTestModel>(json);

            Assert.IsNull(deserialized.NullableId);
            Assert.IsNull(deserialized.NullableString);
            Assert.IsNull(deserialized.NullableDate);
        }

        [TestMethod]
        public void TestInitProperties_PartialJson()
        {
            // Test that missing properties get default values
            string json = @"{
                ""id"": 123
            }";

            var result = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.AreEqual(123, result.Id);
            Assert.IsNull(result.Name);  // String defaults to null
            Assert.AreEqual(0.0, result.Price);  // Double defaults to 0
        }

        [TestMethod]
        public void TestInitProperties_EmptyJson()
        {
            string json = "{}";

            var result = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.AreEqual(0, result.Id);
            Assert.IsNull(result.Name);
            Assert.AreEqual(0.0, result.Price);
        }

        [TestMethod]
        public void TestInitProperties_NullJson()
        {
            string json = "null";

            var result = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestInitProperties_ExtraPropertiesIgnored()
        {
            string json = @"{
                ""id"": 123,
                ""name"": ""Test"",
                ""price"": 99.99,
                ""unknownProperty"": ""should be ignored"",
                ""anotherUnknown"": 12345
            }";

            var result = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.AreEqual(123, result.Id);
            Assert.AreEqual("Test", result.Name);
            Assert.AreEqual(99.99, result.Price);
        }
    }

    [TestClass]
    public class InitPropertyEdgeCaseTests
    {
        [TestMethod]
        public void TestInitProperties_LargeObject()
        {
            var original = new ComplexInitPropertyTestModel
            {
                Id = int.MaxValue,
                Numbers = Enumerable.Range(0, 1000).ToList(),
                Tags = Enumerable.Range(0, 100)
                    .ToDictionary(i => $"key{i}", i => $"value{i}"),
                Nested = new InitPropertyTestModel
                {
                    Id = int.MinValue,
                    Name = new string('x', 1000),
                    Price = double.MaxValue
                }
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<ComplexInitPropertyTestModel>(json);

            Assert.AreEqual(original.Id, deserialized.Id);
            Assert.HasCount(1000, deserialized.Numbers);
            Assert.HasCount(100, deserialized.Tags);
        }

        [TestMethod]
        public void TestInitProperties_SpecialCharactersInStrings()
        {
            var original = new InitPropertyTestModel
            {
                Id = 1,
                Name = "Test\nWith\tSpecial\"Characters",
                Price = 1.23
            };

            string json = GaldrJson.Serialize(original);
            var deserialized = GaldrJson.Deserialize<InitPropertyTestModel>(json);

            Assert.AreEqual(original.Name, deserialized.Name);
        }
    }

    [TestClass]
    public class CircularReferenceTests
    {
        [TestMethod]
        public void TestCircularReference_ThrowsJsonException()
        {
            var person = new CircularPerson { Name = "John" };
            var address = new CircularAddress { Street = "123 Main St", Person = person };
            person.Address = address;  // Creates circular reference

            var exception = Assert.Throws<JsonException>(() =>
            {
                string json = GaldrJson.Serialize(person, new GaldrJsonOptions()
                {
                    DetectCycles = true
                });
            });

            Assert.Contains("cycle", exception.Message);
        }

        [TestMethod]
        public void TestNonCircularReference_Succeeds()
        {
            var person = new CircularPerson
            {
                Name = "John",
                Address = new CircularAddress
                {
                    Street = "123 Main St",
                    Person = null  // No circular reference
                }
            };

            string json = GaldrJson.Serialize(person);
            Assert.IsNotNull(json);
            Assert.Contains("John", json);
        }
    }
}
