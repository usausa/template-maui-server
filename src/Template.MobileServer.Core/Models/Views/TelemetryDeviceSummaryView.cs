namespace Template.MobileServer.Models.Views;

public sealed record TelemetryDeviceSummaryView(
    TelemetryDeviceInfoEntity DeviceInfo,
    IReadOnlyList<TelemetryLatestValueView> LatestValues,
    IReadOnlyList<TelemetryLogSummaryView> LogSummary,
    IReadOnlyList<TelemetryLogEntity> RecentErrors);
