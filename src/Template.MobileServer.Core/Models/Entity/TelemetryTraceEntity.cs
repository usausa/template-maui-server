namespace Template.MobileServer.Models.Entity;

// トレースの集計 (スパンを保存するたびに、そのトレースのスパンから作り直す)。RootName は親の無いスパン、無ければ最初のスパンの名前
[Name("Trace")]
public sealed class TelemetryTraceEntity
{
    [Key]
    public string TraceId { get; set; } = default!;

    public string RootName { get; set; } = default!;

    public long StartTimeUnixNano { get; set; }

    public long EndTimeUnixNano { get; set; }

    public int SpanCount { get; set; }

    public int ErrorCount { get; set; }
}
