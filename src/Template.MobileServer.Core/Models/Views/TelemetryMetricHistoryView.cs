namespace Template.MobileServer.Models.Views;

// メトリクスの推移 (端末の全系列と、範囲の点を時間で束ねた集計)
public sealed record TelemetryMetricHistoryView(
    IReadOnlyList<TelemetryMetricSeriesEntity> Series,
    IReadOnlyList<TelemetryMetricBucketView> Buckets);
