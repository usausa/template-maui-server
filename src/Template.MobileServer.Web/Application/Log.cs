namespace Template.MobileServer.Web.Application;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Service start.")]
    public static partial void InfoServiceStart(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Runtime: os=[{osDescription}], framework=[{frameworkDescription}], rid=[{runtimeIdentifier}]")]
    public static partial void InfoServiceSettingsRuntime(this ILogger logger, string osDescription, string frameworkDescription, string runtimeIdentifier);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment: version=[{version}], directory=[{directory}]")]
    public static partial void InfoServiceSettingsEnvironment(this ILogger logger, Version? version, string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "GCSettings: serverGC=[{isServerGC}], latencyMode=[{latencyMode}], largeObjectHeapCompactionMode=[{largeObjectHeapCompactionMode}]")]
    public static partial void InfoServiceSettingsGC(this ILogger logger, bool isServerGC, GCLatencyMode latencyMode, GCLargeObjectHeapCompactionMode largeObjectHeapCompactionMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "ThreadPool: workerThreads=[{workerThreads}], completionPortThreads=[{completionPortThreads}]")]
    public static partial void InfoServiceSettingsThreadPool(this ILogger logger, int workerThreads, int completionPortThreads);

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry: otelEndPoint=[{otelEndPoint}], prometheusUri=[{prometheusUri}]")]
    public static partial void InfoServiceSettingsTelemetry(this ILogger logger, string otelEndPoint, string prometheusUri);

    // Circuit

    [LoggerMessage(Level = LogLevel.Information, Message = "Circuit opened. id=[{id}], count=[{count}]")]
    public static partial void InfoCircuitOpened(this ILogger logger, string id, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Circuit closed. id=[{id}], count=[{count}]")]
    public static partial void InfoCircuitClosed(this ILogger logger, string id, int count);

    // Request

    [LoggerMessage(Level = LogLevel.Warning, Message = "Long execution. method=[{method}], route=[{route}], elapsed=[{elapsed}]")]
    public static partial void WarnLongExecution(this ILogger logger, string method, string route, long elapsed);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception.")]
    public static partial void ErrorUnhandledException(this ILogger logger, Exception ex);
}
