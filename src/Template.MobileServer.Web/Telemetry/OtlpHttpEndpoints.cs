namespace Template.MobileServer.Web.Telemetry;

using System.IO.Compression;

using Google.Protobuf;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

using Template.MobileServer.Web.Infrastructure.Routing;

// OTLP/HTTP の受信口 (protobuf。gzip の本文も受ける)
public static class OtlpHttpEndpoints
{
    public const string ProtobufContentType = "application/x-protobuf";

    private const int BufferSize = 81920;

    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapOtlpHttpEndpoints(this WebApplication app, int port)
    {
        app.MapPost("/v1/traces", HandleTracesAsync).RequirePort(port).ExcludeFromDescription();
        app.MapPost("/v1/metrics", HandleMetricsAsync).RequirePort(port).ExcludeFromDescription();
        app.MapPost("/v1/logs", HandleLogsAsync).RequirePort(port).ExcludeFromDescription();
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static Task<IResult> HandleTracesAsync(HttpRequest request, OtlpReceiver receiver, TelemetryReceiverOption option) =>
        ReceiveAsync(request, option.MaxReceiveMessageSize, ExportTraceServiceRequest.Parser, receiver.Receive);

    private static Task<IResult> HandleMetricsAsync(HttpRequest request, OtlpReceiver receiver, TelemetryReceiverOption option) =>
        ReceiveAsync(request, option.MaxReceiveMessageSize, ExportMetricsServiceRequest.Parser, receiver.Receive);

    private static Task<IResult> HandleLogsAsync(HttpRequest request, OtlpReceiver receiver, TelemetryReceiverOption option) =>
        ReceiveAsync(request, option.MaxReceiveMessageSize, ExportLogsServiceRequest.Parser, receiver.Receive);

    // 応答は protobuf の Export*ServiceResponse。protobuf 以外は 415、上限を超えたら 413、壊れていたら 400
    internal static async Task<IResult> ReceiveAsync<TRequest, TResponse>(HttpRequest request, int limit, MessageParser<TRequest> parser, Func<TRequest, TResponse> receive)
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage
    {
        if (!String.Equals(request.ContentType, ProtobufContentType, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        TRequest message;
        try
        {
            var body = await ReadBodyAsync(request, limit);
            if (body is null)
            {
                return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            message = parser.ParseFrom(body);
        }
        catch (Exception ex) when (ex is InvalidProtocolBufferException or InvalidDataException)
        {
            return TypedResults.BadRequest();
        }

        return TypedResults.Bytes(receive(message).ToByteArray(), ProtobufContentType);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    // null when the (decompressed) body exceeds the limit
    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, int limit)
    {
        if (!String.Equals(request.Headers.ContentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
        {
            return await ReadAsync(request.Body, limit, request.HttpContext.RequestAborted);
        }

        await using var gzip = new GZipStream(request.Body, CompressionMode.Decompress, true);
        return await ReadAsync(gzip, limit, request.HttpContext.RequestAborted);
    }

    private static async Task<byte[]?> ReadAsync(Stream stream, int limit, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[BufferSize];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > limit)
            {
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }
}
