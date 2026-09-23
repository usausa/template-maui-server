namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Metrics.V1;

public sealed class OtlpMetricsHandler : MetricsService.MetricsServiceBase
{
    private readonly ILogger<OtlpMetricsHandler> log;

    public OtlpMetricsHandler(ILogger<OtlpMetricsHandler> log)
    {
        this.log = log;
    }

    public override Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
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

        return Task.FromResult(new ExportMetricsServiceResponse());
    }
}
