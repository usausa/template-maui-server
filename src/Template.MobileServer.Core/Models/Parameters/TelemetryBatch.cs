namespace Template.MobileServer.Models.Parameters;

// 1 回の Export の 1 端末分 (保存の単位)
public sealed class TelemetryBatch
{
    public required string DeviceId { get; init; }

    public required TelemetryDeviceInfoEntity DeviceInfo { get; init; }

    public required TelemetryResourceEntity Resource { get; init; }

    public IList<TelemetryMetric> Metrics { get; } = [];

    public IList<TelemetrySpanEntity> Spans { get; } = [];

    public IList<TelemetryLogEntity> Logs { get; } = [];
}
