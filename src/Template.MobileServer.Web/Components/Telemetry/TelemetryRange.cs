namespace Template.MobileServer.Web.Components.Telemetry;

public sealed record TelemetryRange(string Key, string Label, TimeSpan Duration, TimeSpan Interval)
{
    private const long NanosecondsPerTick = 100;

    public static IReadOnlyList<TelemetryRange> All { get; } =
    [
        new("15m", "15 分", TimeSpan.FromMinutes(15), TimeSpan.Zero),
        new("1h", "1 時間", TimeSpan.FromHours(1), TimeSpan.Zero),
        new("6h", "6 時間", TimeSpan.FromHours(6), TimeSpan.FromMinutes(1)),
        new("24h", "24 時間", TimeSpan.FromHours(24), TimeSpan.FromMinutes(5)),
        new("7d", "7 日", TimeSpan.FromDays(7), TimeSpan.FromMinutes(30)),
        new("30d", "30 日", TimeSpan.FromDays(30), TimeSpan.FromHours(2))
    ];

    public static TelemetryRange Default => All[1];

    public long DurationNanoseconds => Duration.Ticks * NanosecondsPerTick;

    public long IntervalNanoseconds => Interval > TimeSpan.Zero ? Interval.Ticks * NanosecondsPerTick : 1;

    public bool ShowDate => Duration >= TimeSpan.FromHours(24);

    public static TelemetryRange Find(string? key) => All.FirstOrDefault(x => x.Key == key) ?? Default;

    public static TelemetryRange Covering(TimeSpan elapsed) => All.FirstOrDefault(x => x.Duration >= elapsed) ?? All[^1];
}
