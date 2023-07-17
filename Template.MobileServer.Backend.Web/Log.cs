namespace Template.MobileServer.Backend.Web;

#pragma warning disable SYSLIB1006
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception.")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Get. path=[{path}]")]
    public static partial void InfoGet(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Post. path=[{path}]")]
    public static partial void InfoPost(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delete. path=[{path}]")]
    public static partial void InfoDelete(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No found. path=[{path}]")]
    public static partial void WarnNotFound(this ILogger logger, string? path);
}
#pragma warning restore SYSLIB1006
