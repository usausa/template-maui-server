namespace Template.MobileServer.Models.Views;

// 保存済みの Resource (Id と Hash) と系列
public sealed record TelemetryIdView(
    IReadOnlyList<TelemetryResourceEntity> Resources,
    IReadOnlyList<TelemetryMetricSeriesEntity> Series);
