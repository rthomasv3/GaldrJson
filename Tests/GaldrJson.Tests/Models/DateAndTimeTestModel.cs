namespace GaldrJson.Tests.Models
{
    [GaldrJsonSerializable]
    internal class DateAndTimeTestModel
    {
        public DateTime TestDateTime { get; set; }
        public DateTimeOffset TestDateTimeOffset { get; set; }
        public TimeSpan TestTimeSpan { get; set; }
        public List<TimeSpan> TestTimeSpanList { get; set; }
        public List<DateTime> TestDateTimeList { get; set; }
    }
}
