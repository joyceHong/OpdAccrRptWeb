using OpdAccrRptWeb.Infrastructure;

namespace OpdAccrRptWeb.Tests;

public sealed class TaipeiTimeZoneProviderTests
{
    [Fact]
    public void Resolve_UsesWindowsIdWhenIanaIdIsUnavailable()
    {
        var attempts = new List<string>();
        TimeZoneInfo windowsTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Taipei Standard Time", TimeSpan.FromHours(8), "Taipei", "Taipei");

        TimeZoneInfo result = TaipeiTimeZoneProvider.Resolve(id =>
        {
            attempts.Add(id);
            return id == "Taipei Standard Time"
                ? windowsTimeZone
                : throw new TimeZoneNotFoundException();
        });

        Assert.Same(windowsTimeZone, result);
        Assert.Equal(["Asia/Taipei", "Taipei Standard Time"], attempts);
    }

    [Fact]
    public void Resolve_UsesIanaIdWhenAvailable()
    {
        var attempts = new List<string>();
        TimeZoneInfo ianaTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Taipei", TimeSpan.FromHours(8), "Taipei", "Taipei");

        TimeZoneInfo result = TaipeiTimeZoneProvider.Resolve(id =>
        {
            attempts.Add(id);
            return ianaTimeZone;
        });

        Assert.Same(ianaTimeZone, result);
        Assert.Equal(["Asia/Taipei"], attempts);
    }
}
