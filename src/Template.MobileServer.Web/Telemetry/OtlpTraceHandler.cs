namespace Template.MobileServer.Web.Telemetry;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Trace.V1;

public sealed class OtlpTraceHandler : TraceService.TraceServiceBase
{
    private readonly OtlpReceiver receiver;

    public OtlpTraceHandler(OtlpReceiver receiver)
    {
        this.receiver = receiver;
    }

    public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context) =>
        Task.FromResult(receiver.Receive(request));
}
