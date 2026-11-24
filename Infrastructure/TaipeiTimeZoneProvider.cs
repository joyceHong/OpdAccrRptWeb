namespace OpdAccrRptWeb.Infrastructure;

public static class TaipeiTimeZoneProvider
{
    public static TimeZoneInfo GetTimeZone() => Resolve(TimeZoneInfo.FindSystemTimeZoneById);

    internal static TimeZoneInfo Resolve(Func<string, TimeZoneInfo> findTimeZone)
    {
        foreach (string id in new[] { "Asia/Taipei", "Taipei Standard Time" })
        {
            try
            {
                return findTimeZone(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // The available ID depends on the host operating system and time zone data.
            }
        }

        throw new TimeZoneNotFoundException("找不到台北時區（Asia/Taipei 或 Taipei Standard Time）。");
    }
}
