namespace Template.MobileServer.Components;

using MudBlazor;

using Template.MobileServer.Web.Components;

public sealed class ViewHelperTests
{
    [Theory]
    [InlineData(1.0, TelemetryLevel.Good)]
    [InlineData(0.5, TelemetryLevel.Good)]
    [InlineData(0.49, TelemetryLevel.Warning)]
    [InlineData(0.2, TelemetryLevel.Warning)]
    [InlineData(0.19, TelemetryLevel.Critical)]
    public void BatteryLevelUsesThresholds(double ratio, TelemetryLevel expected)
    {
        // Act / Assert
        Assert.Equal(expected, ViewHelper.BatteryLevel(ratio));
    }

    [Theory]
    [InlineData(-50, TelemetryLevel.Good, 4)]
    [InlineData(-60, TelemetryLevel.Good, 3)]
    [InlineData(-67, TelemetryLevel.Good, 3)]
    [InlineData(-75, TelemetryLevel.Warning, 2)]
    [InlineData(-85, TelemetryLevel.Critical, 1)]
    public void SignalUsesThresholds(double dbm, TelemetryLevel expected, int bars)
    {
        // Arrange
        var icons = new[] { Icons.Material.Filled.NetworkWifi1Bar, Icons.Material.Filled.NetworkWifi2Bar, Icons.Material.Filled.NetworkWifi3Bar, Icons.Material.Filled.SignalWifi4Bar };

        // Act / Assert
        Assert.Equal(expected, ViewHelper.SignalLevel(dbm));
        Assert.Equal(icons[bars - 1], ViewHelper.SignalIcon(dbm));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1_500, "1.5 µs")]
    [InlineData(12_345_678, "12.35 ms")]
    [InlineData(1_500_000_000, "1.5 s")]
    public void FormatDurationUsesUnits(long nanoseconds, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, ViewHelper.FormatDuration(nanoseconds));
    }

    [Theory]
    [InlineData(59, "59 秒前")]
    [InlineData(60, "1 分前")]
    [InlineData(3599, "59 分前")]
    [InlineData(7200, "2 時間前")]
    [InlineData(90000, "1 日前")]
    public void FormatElapsedUsesLargestUnit(int seconds, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, ViewHelper.FormatElapsed(TimeSpan.FromSeconds(seconds)));
    }
}
