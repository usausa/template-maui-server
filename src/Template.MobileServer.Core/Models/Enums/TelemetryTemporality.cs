namespace Template.MobileServer.Models.Enums;

// OTLP の AggregationTemporality と同じ値
public enum TelemetryTemporality
{
    Unspecified,
    Delta,
    Cumulative
}
