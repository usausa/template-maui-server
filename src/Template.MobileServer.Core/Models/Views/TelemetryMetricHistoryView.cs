namespace Template.MobileServer.Models.Views;

public sealed record TelemetryMetricHistoryView(
    IReadOnlyList<TelemetryMetricSeriesEntity> Series,
    IReadOnlyList<TelemetryMetricBucketView> Buckets);
