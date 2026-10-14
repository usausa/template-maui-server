namespace Template.MobileServer.Models.Views;

public sealed record TelemetryIdView(
    IReadOnlyList<TelemetryResourceEntity> Resources,
    IReadOnlyList<TelemetryMetricSeriesEntity> Series);
