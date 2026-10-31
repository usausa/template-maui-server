namespace Template.MobileServer.Models.Results;

public sealed record TelemetryDeviceSummaryResult(
    TelemetryDeviceInfoEntity DeviceInfo,
    IReadOnlyList<TelemetryLatestValueView> LatestValues,
    IReadOnlyList<TelemetryLogSummaryView> LogSummary,
    IReadOnlyList<TelemetryLogEntity> RecentErrors);
