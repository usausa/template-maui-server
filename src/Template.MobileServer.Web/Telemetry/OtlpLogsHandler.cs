namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Logs.V1;

public sealed class OtlpLogsHandler : LogsService.LogsServiceBase
{
    private readonly OtlpReceiver receiver;

    public OtlpLogsHandler(OtlpReceiver receiver)
    {
        this.receiver = receiver;
    }

    public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context) =>
        Task.FromResult(receiver.Receive(request));
}
