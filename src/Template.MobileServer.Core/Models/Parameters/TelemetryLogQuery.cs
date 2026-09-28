namespace Template.MobileServer.Models.Parameters;

// ログの照会の条件。新しい順に Limit 件ずつ読み、続きは前の最後のログの時刻と Id (Before*) から
public sealed class TelemetryLogQuery
{
    // 時刻 (UTC の Unix ナノ秒) 以降
    public long Since { get; init; }

    // 重大度 (SeverityNumber) 以上。0 なら全部
    public int Severity { get; init; }

    // 本文の部分一致
    public string? Body { get; init; }

    public string? TraceId { get; init; }

    public long? BeforeTime { get; init; }

    public long BeforeId { get; init; }

    public int Limit { get; init; }
}
