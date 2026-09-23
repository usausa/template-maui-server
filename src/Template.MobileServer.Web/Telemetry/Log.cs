namespace Template.MobileServer.Web.Telemetry;

using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Trace.V1;

internal static partial class Log
{
    // Receive

    [LoggerMessage(Level = LogLevel.Information, Message = "Traces received. service=[{service}], device=[{device}], spans=[{spans}]")]
    public static partial void InfoTracesReceived(this ILogger logger, string service, string device, int spans);

    [LoggerMessage(Level = LogLevel.Information, Message = "Metrics received. service=[{service}], device=[{device}], metrics=[{metrics}], points=[{points}]")]
    public static partial void InfoMetricsReceived(this ILogger logger, string service, string device, int metrics, int points);

    [LoggerMessage(Level = LogLevel.Information, Message = "Logs received. service=[{service}], device=[{device}], records=[{records}]")]
    public static partial void InfoLogsReceived(this ILogger logger, string service, string device, int records);

    // Content

    [LoggerMessage(Level = LogLevel.Debug, Message = "Resource. attributes=[{attributes}]")]
    public static partial void DebugResource(this ILogger logger, string attributes);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Span. scope=[{scope}], name=[{name}], kind=[{kind}], traceId=[{traceId}], spanId=[{spanId}], parentSpanId=[{parentSpanId}], duration=[{duration}], status=[{status}], attributes=[{attributes}]")]
    public static partial void DebugSpan(this ILogger logger, string scope, string name, Span.Types.SpanKind kind, string traceId, string spanId, string parentSpanId, TimeSpan duration, Status.Types.StatusCode status, string attributes);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Metric. scope=[{scope}], name=[{name}], unit=[{unit}], type=[{type}], temporality=[{temporality}], points=[{points}]")]
    public static partial void DebugMetric(this ILogger logger, string scope, string name, string unit, Metric.DataOneofCase type, AggregationTemporality temporality, string points);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Log record. scope=[{scope}], time=[{time}], severity=[{severity}], event=[{eventName}], body=[{body}], traceId=[{traceId}], spanId=[{spanId}], attributes=[{attributes}]")]
    public static partial void DebugLogRecord(this ILogger logger, string scope, DateTimeOffset time, SeverityNumber severity, string eventName, string body, string traceId, string spanId, string attributes);
}
