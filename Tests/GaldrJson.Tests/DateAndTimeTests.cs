using GaldrJson.Tests.Models;

namespace GaldrJson.Tests;

[TestClass]
public class DateAndTimeTests
{
    [TestMethod]
    public void TestDatesAndTimeSerialize()
    {
        DateAndTimeTestModel model = new()
        {
            TestDateTime = new DateTime(2024, 6, 20, 14, 30, 0, DateTimeKind.Utc),
            TestDateTimeOffset = new DateTimeOffset(2024, 6, 20, 14, 30, 0, TimeSpan.FromHours(-5)),
            TestTimeSpan = TimeSpan.FromHours(5),
            TestDateTimeList = new List<DateTime>
            {
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            },
            TestTimeSpanList = new List<TimeSpan>
            {
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(2)
            }
        };

        string json = GaldrJson.Serialize(model);

        Assert.IsNotNull(json);
    }
}
