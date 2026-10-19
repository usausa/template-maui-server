namespace Template.MobileServer.Models.Entity;

[Name("MetricSeries")]
public sealed class TelemetryMetricSeriesEntity
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public string ScopeName { get; set; } = default!;

    public string Unit { get; set; } = default!;

    public string Kind { get; set; } = TelemetryMetricKind.Unspecified;

    public string Temporality { get; set; } = TelemetryTemporality.Unspecified;

    public bool IsMonotonic { get; set; }

    public string AttributesJson { get; set; } = default!;
}
