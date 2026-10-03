namespace Template.MobileServer.Web.Hubs;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Monitor connected. id=[{connectionId}], connections=[{count}]")]
    public static partial void InfoMonitorConnected(this ILogger logger, string connectionId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitor disconnected. id=[{connectionId}], connections=[{count}]")]
    public static partial void InfoMonitorDisconnected(this ILogger logger, string connectionId, int count, Exception? exception);

    // Push

    [LoggerMessage(Level = LogLevel.Information, Message = "Push connected. id=[{connectionId}], device=[{device}], pending=[{pending}]")]
    public static partial void InfoPushConnected(this ILogger logger, string connectionId, string device, int pending);

    [LoggerMessage(Level = LogLevel.Information, Message = "Push disconnected. id=[{connectionId}], device=[{device}]")]
    public static partial void InfoPushDisconnected(this ILogger logger, string connectionId, string? device, Exception? exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push rejected. id=[{connectionId}], device=[{device}]")]
    public static partial void WarnPushRejected(this ILogger logger, string connectionId, string? device);
}
