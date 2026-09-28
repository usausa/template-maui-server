namespace Template.MobileServer.Web.Components.Telemetry;

// ログのタブの重大度の絞り込み (Severity 以上)
public sealed record TelemetryLogLevel(string Key, string Label, int Severity)
{
    public static IReadOnlyList<TelemetryLogLevel> All { get; } =
    [
        new("all", "すべて", 0),
        new("info", "INFO 以上", TelemetrySeverity.Info),
        new("warn", "WARN 以上", TelemetrySeverity.Warn),
        new("error", "ERROR 以上", TelemetrySeverity.Error),
        new("fatal", "FATAL", TelemetrySeverity.Fatal)
    ];

    public static TelemetryLogLevel Default => All[0];

    public static TelemetryLogLevel Error => All[3];

    public static TelemetryLogLevel Find(string? key) => All.FirstOrDefault(x => x.Key == key) ?? Default;
}
