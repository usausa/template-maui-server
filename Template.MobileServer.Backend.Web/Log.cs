namespace Template.MobileServer.Backend.Web;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Service start.")]
    public static partial void InfoServiceStart(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment. version=[{version}], runtime=[{runtime}], directory=[{directory}]")]
    public static partial void InfoServiceSettingsEnvironment(this ILogger logger, Version? version, Version runtime, string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "GCSettings. serverGC=[{isServerGC}], latencyMode=[{latencyMode}], largeObjectHeapCompactionMode=[{largeObjectHeapCompactionMode}]")]
    public static partial void InfoServiceSettingsGC(this ILogger logger, bool isServerGC, GCLatencyMode latencyMode, GCLargeObjectHeapCompactionMode largeObjectHeapCompactionMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "ThreadPool. workerThreads=[{workerThreads}], completionPortThreads=[{completionPortThreads}]")]
    public static partial void InfoServiceSettingsThreadPool(this ILogger logger, int workerThreads, int completionPortThreads);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception.")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex);

    // Service

    [LoggerMessage(Level = LogLevel.Information, Message = "Get. path=[{path}]")]
    public static partial void InfoGet(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Post. path=[{path}]")]
    public static partial void InfoPost(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delete. path=[{path}]")]
    public static partial void InfoDelete(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No found. path=[{path}]")]
    public static partial void WarnNotFound(this ILogger logger, string? path);
}
