namespace Template.MobileServer.Models.Entity;

// 計器と属性の組み合わせ (系列)。属性はキー順に並べた JSON
[Name("MetricSeries")]
public sealed class TelemetryMetricSeriesEntity
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public string ScopeName { get; set; } = default!;

    public string Unit { get; set; } = default!;

    public TelemetryMetricKind Kind { get; set; }

    public TelemetryTemporality Temporality { get; set; }

    public bool IsMonotonic { get; set; }

    public string AttributesJson { get; set; } = default!;
}
