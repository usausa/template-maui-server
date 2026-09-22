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
}
