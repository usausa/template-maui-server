namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Logs.V1;

public sealed class OtlpLogsHandler : LogsService.LogsServiceBase
{
    private readonly ILogger<OtlpLogsHandler> log;

    public OtlpLogsHandler(ILogger<OtlpLogsHandler> log)
    {
        this.log = log;
    }

    public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
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

        return Task.FromResult(new ExportLogsServiceResponse());
    }
}
