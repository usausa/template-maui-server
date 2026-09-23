namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Trace.V1;

public sealed class OtlpTraceHandler : TraceService.TraceServiceBase
{
    private readonly ILogger<OtlpTraceHandler> log;

    public OtlpTraceHandler(ILogger<OtlpTraceHandler> log)
    {
        this.log = log;
    }

    public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
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

        return Task.FromResult(new ExportTraceServiceResponse());
    }
}
