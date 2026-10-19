namespace Template.MobileServer.Models.Views;

public sealed record TelemetryTraceDetailView(
    TelemetryTraceEntity Trace,
    IReadOnlyList<TelemetrySpanEntity> Spans,
    IReadOnlyList<TelemetryLogEntity> Logs,
    IReadOnlyList<TelemetryResourceEntity> Resources);
