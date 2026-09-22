namespace Template.MobileServer.Web.Hubs;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Monitor connected. id=[{connectionId}], connections=[{count}]")]
    public static partial void InfoMonitorConnected(this ILogger logger, string connectionId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitor disconnected. id=[{connectionId}], connections=[{count}]")]
    public static partial void InfoMonitorDisconnected(this ILogger logger, string connectionId, int count, Exception? exception);
}
