namespace Template.MobileServer.Web.Telemetry;

internal static partial class Log
{
    // Receive

    [LoggerMessage(Level = LogLevel.Debug, Message = "Traces received. service=[{service}], device=[{device}], spans=[{spans}], saved=[{saved}]")]
    public static partial void DebugTracesReceived(this ILogger logger, string service, string device, int spans, int saved);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Metrics received. service=[{service}], device=[{device}], points=[{points}], saved=[{saved}]")]
    public static partial void DebugMetricsReceived(this ILogger logger, string service, string device, int points, int saved);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Logs received. service=[{service}], device=[{device}], records=[{records}], saved=[{saved}]")]
    public static partial void DebugLogsReceived(this ILogger logger, string service, string device, int records, int saved);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Telemetry rejected. service=[{service}], device=[{device}], items=[{items}]")]
    public static partial void WarnTelemetryRejected(this ILogger logger, string service, string device, int items);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Telemetry of disabled device. service=[{service}], device=[{device}], items=[{items}]")]
    public static partial void DebugTelemetryDisabled(this ILogger logger, string service, string device, int items);

    [LoggerMessage(Level = LogLevel.Error, Message = "Telemetry save failed. device=[{device}]")]
    public static partial void ErrorTelemetrySaveFailed(this ILogger logger, string device, Exception ex);

    // Device

    [LoggerMessage(Level = LogLevel.Information, Message = "Device registered. device=[{device}], name=[{name}]")]
    public static partial void InfoDeviceRegistered(this ILogger logger, string device, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Telemetry load failed. device=[{device}]")]
    public static partial void ErrorTelemetryLoadFailed(this ILogger logger, string device, Exception ex);

    // Bus

    [LoggerMessage(Level = LogLevel.Error, Message = "Telemetry handler failed.")]
    public static partial void ErrorTelemetryHandlerFailed(this ILogger logger, Exception ex);
}
