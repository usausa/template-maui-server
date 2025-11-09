namespace Template.MobileServer.Web.Api.Rest.Controllers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Get. path=[{path}]")]
    public static partial void InfoGet(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Post. path=[{path}]")]
    public static partial void InfoPost(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delete. path=[{path}]")]
    public static partial void InfoDelete(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No found. path=[{path}]")]
    public static partial void WarnNotFound(this ILogger logger, string? path);
}
