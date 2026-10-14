namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Metrics.V1;

public sealed class OtlpMetricsHandler : MetricsService.MetricsServiceBase
{
    private OtlpReceiver Receiver { get; }

    public OtlpMetricsHandler(OtlpReceiver receiver)
    {
        Receiver = receiver;
    }

    // 保存できなければ UNAVAILABLE (端末は送り直す)
    public override async Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
    {
        try
        {
            return await Receiver.ReceiveAsync(request);
        }
        catch (Exception ex) when (OtlpReceiver.IsStorageError(ex))
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Telemetry storage is unavailable."));
        }
    }
}
