namespace Template.MobileServer.Models.Parameters;

public sealed class TelemetryLogQuery
{
    public long Since { get; init; }

    public int Severity { get; init; }

    public string? Body { get; init; }

    public string? TraceId { get; init; }

    public long? BeforeTime { get; init; }

    public long BeforeId { get; init; }

    public int Limit { get; init; }
}
