namespace Template.MobileServer.Web.Components;

internal static partial class Log
{
    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception.")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex);
}
