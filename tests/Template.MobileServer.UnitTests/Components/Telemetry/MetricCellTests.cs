namespace Template.MobileServer.Components.Telemetry;

using Bunit;

using Template.MobileServer.Web.Components.Telemetry;
using Template.MobileServer.Web.Telemetry;

public sealed class MetricCellTests : MudBlazorTestBase
{
    private static readonly long Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;

    // 電池: 横棒の幅と色、ホバーで値と時刻
    [Fact]
    public void BatteryShowsMeter()
    {
        // Act
        var cut = Render<MetricCell>(parameters => parameters
            .Add(static x => x.Value, new TelemetryValue(Time, 0.15))
            .Add(static x => x.Display, MetricDisplay.Battery));

        // Assert
        var fill = cut.Find(".meter-fill");
        Assert.Contains("meter-critical", fill.ClassName, StringComparison.Ordinal);
        Assert.Equal("width: 15%", fill.GetAttribute("style"));
        Assert.StartsWith("15% (", cut.Find(".metric-cell").GetAttribute("title"), StringComparison.Ordinal);
    }

    // 無線 LAN: アイコンだけを出し、値はホバーで出す
    [Fact]
    public void SignalShowsIconWithValueOnHover()
    {
        // Act
        var cut = Render<MetricCell>(parameters => parameters
            .Add(static x => x.Value, new TelemetryValue(Time, -50))
            .Add(static x => x.Display, MetricDisplay.Signal));

        // Assert
        Assert.NotNull(cut.Find("svg"));
        Assert.Equal(string.Empty, cut.Find(".metric-cell").TextContent.Trim());
        Assert.StartsWith("-50 dBm (", cut.Find(".metric-cell").GetAttribute("title"), StringComparison.Ordinal);
    }

    // アプリケーション固有値: 0〜100 の横棒 (良し悪しの色は付けない) と値
    [Fact]
    public void CustomValueShowsMeter()
    {
        // Act
        var cut = Render<MetricCell>(parameters => parameters
            .Add(static x => x.Value, new TelemetryValue(Time, 42))
            .Add(static x => x.Display, MetricDisplay.CustomValue));

        // Assert
        var fill = cut.Find(".meter-fill");
        Assert.Contains("meter-none", fill.ClassName, StringComparison.Ordinal);
        Assert.Equal("width: 42%", fill.GetAttribute("style"));
        Assert.Equal("42", cut.Find(".metric-cell").TextContent.Trim());
        Assert.StartsWith("42 (", cut.Find(".metric-cell").GetAttribute("title"), StringComparison.Ordinal);
    }

    // 値が無いときは "-"
    [Fact]
    public void MissingValueShowsDash()
    {
        // Act
        var cut = Render<MetricCell>(parameters => parameters
            .Add(static x => x.Display, MetricDisplay.Signal));

        // Assert
        Assert.Equal("-", cut.Find(".metric-cell").TextContent.Trim());
        Assert.Null(cut.Find(".metric-cell").GetAttribute("title"));
    }
}
