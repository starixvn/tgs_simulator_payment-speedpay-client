namespace Payment.SpeedPay;
public static class TimeProducer
{
    public static DateTimeOffset GetTimeByTimeZone(string timeZone)
    {
        var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.UtcNow.UtcDateTime, timezoneInfo);

        return datetime;
    }
}
