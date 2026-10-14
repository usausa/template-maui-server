namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Trace.V1;

public sealed class OtlpTraceHandler : TraceService.TraceServiceBase
{
    private OtlpReceiver Receiver { get; }

    public OtlpTraceHandler(OtlpReceiver receiver)
    {
        Receiver = receiver;
    }

    // 保存できなければ UNAVAILABLE (端末は送り直す)
    public override async Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
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
