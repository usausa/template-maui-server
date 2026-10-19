namespace Template.MobileServer.Models.Views;

public sealed record TelemetryMetricBucketView(
    long SeriesId,
    long BucketUnixNano,
    int Points,
    int ValueCount,
    double? TotalValue,
    long Duration,
    long? Count,
    double? Sum,
    double? Max,
    long LastUnixNano);
