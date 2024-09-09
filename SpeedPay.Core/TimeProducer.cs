namespace Payment.SpeedPay;
internal static class TimeProducer
{
    public static DateTimeOffset GetTimeByTimeZone(string timeZone)
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone!);

        return datetime;
    }
}
