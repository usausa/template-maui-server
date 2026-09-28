namespace Template.MobileServer.Models.Entity;

// スパン。ID は小文字の 16 進 (親が無ければ空文字)、イベントとリンクは JSON (無ければ null)
[Name("Span")]
public sealed class TelemetrySpanEntity
{
    [Key]
    public string TraceId { get; set; } = default!;

    [Key]
    public string SpanId { get; set; } = default!;

    public string ParentSpanId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public TelemetrySpanKind Kind { get; set; }

    public long StartTimeUnixNano { get; set; }

    public long EndTimeUnixNano { get; set; }

    public TelemetryStatusCode StatusCode { get; set; }

    public string StatusMessage { get; set; } = default!;

    public string ScopeName { get; set; } = default!;

    public long ResourceId { get; set; }

    public string AttributesJson { get; set; } = default!;

    public string? EventsJson { get; set; }

    public string? LinksJson { get; set; }
}
