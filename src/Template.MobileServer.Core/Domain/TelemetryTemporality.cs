namespace Template.MobileServer.Domain;

// OTLP の AggregationTemporality と同じ名前
public static class TelemetryTemporality
{
    public const string Unspecified = "Unspecified";

    public const string Delta = "Delta";

    public const string Cumulative = "Cumulative";
}
