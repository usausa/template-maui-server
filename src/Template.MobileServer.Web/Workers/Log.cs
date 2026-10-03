namespace Template.MobileServer.Web.Workers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Worker disabled. worker=[{worker}]")]
    public static partial void InfoWorkerDisabled(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker start. worker=[{worker}]")]
    public static partial void InfoWorkerStart(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker stop. worker=[{worker}]")]
    public static partial void InfoWorkerStop(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Error, Message = "Worker exception. worker=[{worker}]")]
    public static partial void ErrorWorkerException(this ILogger logger, string worker, Exception ex);

    // Telemetry retention

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry expired. device=[{device}], rows=[{rows}]")]
    public static partial void InfoTelemetryExpired(this ILogger logger, string device, int rows);

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry file deleted. device=[{device}]")]
    public static partial void InfoTelemetryFileDeleted(this ILogger logger, string device);

    // Push retention

    [LoggerMessage(Level = LogLevel.Information, Message = "Push expired. rows=[{rows}]")]
    public static partial void InfoPushExpired(this ILogger logger, int rows);
}
