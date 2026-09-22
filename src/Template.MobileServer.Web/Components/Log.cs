namespace Template.MobileServer.Web.Components;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled component exception.")]
    public static partial void ErrorComponentException(this ILogger logger, Exception ex);
}
