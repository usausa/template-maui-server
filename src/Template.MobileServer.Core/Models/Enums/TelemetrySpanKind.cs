namespace Template.MobileServer.Models.Enums;

// OTLP の SpanKind と同じ値
public enum TelemetrySpanKind
{
    Unspecified,
    Internal,
    Server,
    Client,
    Producer,
    Consumer
}
