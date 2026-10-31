namespace Template.MobileServer.Models.Results;

public sealed record TelemetryMetricHistoryResult(
    IReadOnlyList<TelemetryMetricSeriesEntity> Series,
    IReadOnlyList<TelemetryMetricBucketView> Buckets);
