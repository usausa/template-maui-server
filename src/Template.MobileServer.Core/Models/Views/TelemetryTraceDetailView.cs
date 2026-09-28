namespace Template.MobileServer.Models.Views;

// トレースと、そのスパン (開始の順)・ログ (時刻の順)・スパンの Resource
public sealed record TelemetryTraceDetailView(
    TelemetryTraceEntity Trace,
    IReadOnlyList<TelemetrySpanEntity> Spans,
    IReadOnlyList<TelemetryLogEntity> Logs,
    IReadOnlyList<TelemetryResourceEntity> Resources);
