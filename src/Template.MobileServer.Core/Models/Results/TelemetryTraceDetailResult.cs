namespace Template.MobileServer.Models.Results;

public sealed record TelemetryTraceDetailResult(
    TelemetryTraceEntity Trace,
    IReadOnlyList<TelemetrySpanEntity> Spans,
    IReadOnlyList<TelemetryLogEntity> Logs,
    IReadOnlyList<TelemetryResourceEntity> Resources);
