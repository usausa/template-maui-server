namespace Template.MobileServer.Models.Parameters;

public sealed class TelemetryMetric
{
    public required TelemetryMetricSeriesEntity Series { get; init; }

    public IList<TelemetryMetricPointEntity> Points { get; } = [];
}
