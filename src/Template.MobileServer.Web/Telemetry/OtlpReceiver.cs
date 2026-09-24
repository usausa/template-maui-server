namespace Template.MobileServer.Web.Telemetry;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

// OTLP の受信 (gRPC と HTTP で共通)。受信内容はログに出す
public sealed class OtlpReceiver
{
    private readonly ILogger<OtlpReceiver> log;

    public OtlpReceiver(ILogger<OtlpReceiver> log)
    {
        this.log = log;
    }

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    public ExportTraceServiceResponse Receive(ExportTraceServiceRequest request)
    {
        foreach (var resourceSpans in request.ResourceSpans)
        {
            var resource = resourceSpans.Resource;

            // TODO
            if (log.IsEnabled(LogLevel.Information))
            {
                log.InfoTracesReceived(resource.GetServiceName(), resource.GetDeviceId(), resourceSpans.ScopeSpans.Sum(static x => x.Spans.Count));
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.DebugResource(resource?.Attributes.FormatAttributes() ?? string.Empty);
                foreach (var scopeSpans in resourceSpans.ScopeSpans)
                {
                    var scope = scopeSpans.Scope?.Name ?? string.Empty;
                    foreach (var span in scopeSpans.Spans)
                    {
                        log.DebugSpan(scope, span.Name, span.Kind, span.TraceId.ToHex(), span.SpanId.ToHex(), span.ParentSpanId.ToHex(), OtlpHelper.ToDuration(span.StartTimeUnixNano, span.EndTimeUnixNano), span.Status?.Code ?? default, span.Attributes.FormatAttributes());
                    }
                }
            }
        }

        return new ExportTraceServiceResponse();
    }

    //--------------------------------------------------------------------------------
    // Metrics
    //--------------------------------------------------------------------------------

    public ExportMetricsServiceResponse Receive(ExportMetricsServiceRequest request)
    {
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var resource = resourceMetrics.Resource;

            // TODO
            if (log.IsEnabled(LogLevel.Information))
            {
                log.InfoMetricsReceived(resource.GetServiceName(), resource.GetDeviceId(), resourceMetrics.ScopeMetrics.Sum(static x => x.Metrics.Count), resourceMetrics.ScopeMetrics.Sum(static x => x.Metrics.Sum(static y => y.GetPointCount())));
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.DebugResource(resource?.Attributes.FormatAttributes() ?? string.Empty);
                foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
                {
                    var scope = scopeMetrics.Scope?.Name ?? string.Empty;
                    foreach (var metric in scopeMetrics.Metrics)
                    {
                        log.DebugMetric(scope, metric.Name, metric.Unit, metric.DataCase, metric.GetTemporality(), metric.FormatPoints());
                    }
                }
            }
        }

        return new ExportMetricsServiceResponse();
    }

    //--------------------------------------------------------------------------------
    // Logs
    //--------------------------------------------------------------------------------

    public ExportLogsServiceResponse Receive(ExportLogsServiceRequest request)
    {
        foreach (var resourceLogs in request.ResourceLogs)
        {
            var resource = resourceLogs.Resource;

            // TODO
            if (log.IsEnabled(LogLevel.Information))
            {
                log.InfoLogsReceived(resource.GetServiceName(), resource.GetDeviceId(), resourceLogs.ScopeLogs.Sum(static x => x.LogRecords.Count));
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.DebugResource(resource?.Attributes.FormatAttributes() ?? string.Empty);
                foreach (var scopeLogs in resourceLogs.ScopeLogs)
                {
                    var scope = scopeLogs.Scope?.Name ?? string.Empty;
                    foreach (var record in scopeLogs.LogRecords)
                    {
                        // Time is optional, the observed time is always set by the SDK
                        var time = OtlpHelper.ToDateTimeOffset(record.TimeUnixNano > 0 ? record.TimeUnixNano : record.ObservedTimeUnixNano);
                        log.DebugLogRecord(scope, time, record.SeverityNumber, record.EventName, record.Body.FormatValue(), record.TraceId.ToHex(), record.SpanId.ToHex(), record.Attributes.FormatAttributes());
                    }
                }
            }
        }

        return new ExportLogsServiceResponse();
    }
}
