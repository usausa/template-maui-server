namespace Template.MobileServer.Components.Telemetry;

using Bunit;

using Template.MobileServer.Web.Components.Telemetry;
using Template.MobileServer.Web.Telemetry;

public sealed class MetricCellTests : MudBlazorTestBase
{
    private static readonly long Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;

    [Fact]
    public void BatteryShowsIconWithValueOnHover()
    {
        // Act
        var cut = Render<MetricCell>(parameters => parameters
            .Add(static x => x.Value, new TelemetryValue(Time, 0.15))
            .Add(static x => x.Display, MetricDisplay.Battery));

        // Assert
        Assert.Contains("mud-error-text", cut.Find("svg").GetAttribute("class"), StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".meter-fill"));
        Assert.Equal(string.Empty, cut.Find(".metric-cell").TextContent.Trim());
        Assert.StartsWith("15% (", cut.Find(".metric-cell").GetAttribute("title"), StringComparison.Ordinal);
    }

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
        Assert.Equal("42%", fill.GetAttribute("width"));
        Assert.Equal("42", cut.Find(".metric-cell").TextContent.Trim());
        Assert.StartsWith("42 (", cut.Find(".metric-cell").GetAttribute("title"), StringComparison.Ordinal);
    }

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
