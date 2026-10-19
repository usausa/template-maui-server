namespace Template.MobileServer.Models.Entity;

[Name("MetricPoint")]
public sealed class TelemetryMetricPointEntity
{
    [Key]
    public long SeriesId { get; set; }

    [Key]
    public long TimeUnixNano { get; set; }

    public long StartTimeUnixNano { get; set; }

    public double? Value { get; set; }

    public long? Count { get; set; }

    public double? Sum { get; set; }

    public double? Min { get; set; }

    public double? Max { get; set; }

    public string? Detail { get; set; }
}
