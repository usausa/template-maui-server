namespace Template.MobileServer.Web.Services;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Notify failed.")]
    public static partial void ErrorNotifyFailed(this ILogger logger, Exception ex);
}
