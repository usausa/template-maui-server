namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Metrics.V1;

public sealed class OtlpMetricsHandler : MetricsService.MetricsServiceBase
{
    private readonly OtlpReceiver receiver;

    public OtlpMetricsHandler(OtlpReceiver receiver)
    {
        this.receiver = receiver;
    }

    public override Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context) =>
        Task.FromResult(receiver.Receive(request));
}
