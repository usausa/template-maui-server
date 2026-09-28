namespace Template.MobileServer.Models.Entity;

// ログ。Hash は送り直しの重複を見分けるための内容のハッシュ (時刻と組で一意)
[Name("Logs")]
public sealed class TelemetryLogEntity
{
    [Key]
    public long Id { get; set; }

    public long TimeUnixNano { get; set; }

    public long ObservedTimeUnixNano { get; set; }

    public int SeverityNumber { get; set; }

    public string SeverityText { get; set; } = default!;

    public string EventName { get; set; } = default!;

    public string Body { get; set; } = default!;

    public string TraceId { get; set; } = default!;

    public string SpanId { get; set; } = default!;

    public string ScopeName { get; set; } = default!;

    public long ResourceId { get; set; }

    public string AttributesJson { get; set; } = default!;

    public long Hash { get; set; }
}
