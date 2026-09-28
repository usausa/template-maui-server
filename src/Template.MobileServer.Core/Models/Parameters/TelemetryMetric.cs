namespace Template.MobileServer.Models.Parameters;

// 1 つの系列と、その点
public sealed class TelemetryMetric
{
    public required TelemetryMetricSeriesEntity Series { get; init; }

    public IList<TelemetryMetricPointEntity> Points { get; } = [];
}
