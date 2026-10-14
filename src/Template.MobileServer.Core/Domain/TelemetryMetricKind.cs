namespace Template.MobileServer.Domain;

public static class TelemetryMetricKind
{
    public const string Unspecified = "Unspecified";

    public const string Gauge = "Gauge";

    public const string Sum = "Sum";

    public const string Histogram = "Histogram";

    public const string ExponentialHistogram = "ExponentialHistogram";

    public const string Summary = "Summary";
}
