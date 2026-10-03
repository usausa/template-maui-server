namespace Template.MobileServer.Web.Services;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Notify failed.")]
    public static partial void ErrorNotifyFailed(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Push sent. target=[{target}], count=[{count}]")]
    public static partial void InfoPushSent(this ILogger logger, string target, int count);
}
