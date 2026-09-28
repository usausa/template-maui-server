namespace Template.MobileServer.Web.Components.Telemetry;

// テレメトリ画面の URL (相対) とタブのクエリの値
public static class TelemetryLinks
{
    public const string MetricsTab = "metrics";

    public const string TracesTab = "traces";

    public const string LogsTab = "logs";

    public static string Device(string deviceId) => $"telemetry/{Uri.EscapeDataString(deviceId)}";

    public static string Logs(string deviceId, TelemetryRange range, TelemetryLogLevel level) =>
        $"{Device(deviceId)}?tab={LogsTab}&range={range.Key}&level={level.Key}";
}
