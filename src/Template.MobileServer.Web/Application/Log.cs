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

    // Worker

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker disabled. worker=[{worker}]")]
    public static partial void InfoWorkerDisabled(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker start. worker=[{worker}]")]
    public static partial void InfoWorkerStart(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker stop. worker=[{worker}]")]
    public static partial void InfoWorkerStop(this ILogger logger, string worker);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception.")]
    public static partial void ErrorUnhandledException(this ILogger logger, Exception ex);
}
