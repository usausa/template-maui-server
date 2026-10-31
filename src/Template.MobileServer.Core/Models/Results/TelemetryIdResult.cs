namespace Template.MobileServer.Models.Results;

public sealed record TelemetryIdResult(
    IReadOnlyList<TelemetryResourceEntity> Resources,
    IReadOnlyList<TelemetryMetricSeriesEntity> Series);
