namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Logs.V1;

public sealed class OtlpLogsHandler : LogsService.LogsServiceBase
{
    private OtlpReceiver Receiver { get; }

    public OtlpLogsHandler(OtlpReceiver receiver)
    {
        Receiver = receiver;
    }

    // 保存できなければ UNAVAILABLE (端末は送り直す)
    public override async Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
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
