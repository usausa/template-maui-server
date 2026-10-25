namespace Template.MobileServer.Web.Components.Telemetry;

public static class TelemetryLinks
{
    public const string MetricsTab = "metrics";

    public const string TracesTab = "traces";

    public const string LogsTab = "logs";

    public static string Device(string terminalId) => $"telemetry/{Uri.EscapeDataString(terminalId)}";

    public static string Logs(string terminalId, TelemetryRange range, TelemetryLogLevel level) =>
        $"{Device(terminalId)}?tab={LogsTab}&range={range.Key}&level={level.Key}";
}
