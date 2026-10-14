namespace Template.MobileServer.Domain;

// OTLP の SpanKind と同じ名前
public static class TelemetrySpanKind
{
    public const string Unspecified = "Unspecified";

    public const string Internal = "Internal";

    public const string Server = "Server";

    public const string Client = "Client";

    public const string Producer = "Producer";

    public const string Consumer = "Consumer";
}
