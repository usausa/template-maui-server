namespace Template.MobileServer.Models.Parameters;

public sealed class TelemetryBatch
{
    public required string TerminalId { get; init; }

    public required TelemetryDeviceInfoEntity DeviceInfo { get; init; }

    public required TelemetryResourceEntity Resource { get; init; }

    public IList<TelemetryMetric> Metrics { get; } = [];

    public IList<TelemetrySpanEntity> Spans { get; } = [];

    public IList<TelemetryLogEntity> Logs { get; } = [];
}
