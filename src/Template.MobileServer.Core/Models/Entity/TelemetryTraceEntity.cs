namespace Template.MobileServer.Models.Entity;

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
