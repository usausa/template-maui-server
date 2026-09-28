namespace Template.MobileServer.Models.Views;

// 端末のテレメトリの要約 (起動時にダッシュボード用のキャッシュを作る)
public sealed record TelemetryDeviceSummaryView(
    TelemetryDeviceInfoEntity DeviceInfo,
    IReadOnlyList<TelemetryLatestValueView> LatestValues,
    IReadOnlyList<TelemetryLogSummaryView> LogSummary,
    IReadOnlyList<TelemetryLogEntity> RecentErrors);
