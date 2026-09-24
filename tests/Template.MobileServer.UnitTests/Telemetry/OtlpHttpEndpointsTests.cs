namespace Template.MobileServer.Telemetry;

using System.IO.Compression;

using Google.Protobuf;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;

using Template.MobileServer.Web.Telemetry;

public sealed class OtlpHttpEndpointsTests
{
    private const int Limit = 1024 * 1024;

    // protobuf の本文を受け、protobuf の応答を返す
    [Fact]
    public async Task ReceiveAcceptsProtobuf()
    {
        // Arrange
        var request = CreateRequest(CreateMessage().ToByteArray());
        var received = default(ExportLogsServiceRequest);

        // Act
        var result = await OtlpHttpEndpoints.ReceiveAsync(request, Limit, ExportLogsServiceRequest.Parser, x =>
        {
            received = x;
            return new ExportLogsServiceResponse();
        });

        // Assert
        var content = Assert.IsType<FileContentHttpResult>(result);
        Assert.Equal(OtlpHttpEndpoints.ProtobufContentType, content.ContentType);
        Assert.Equal(2, received?.ResourceLogs[0].ScopeLogs[0].LogRecords.Count);
    }

    // gzip の本文は展開する
    [Fact]
    public async Task ReceiveAcceptsGzip()
    {
        // Arrange
        using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionMode.Compress, true))
        {
            CreateMessage().WriteTo(gzip);
        }

        var request = CreateRequest(compressed.ToArray());
        request.Headers.ContentEncoding = "gzip";
        var received = default(ExportLogsServiceRequest);

        // Act
        var result = await OtlpHttpEndpoints.ReceiveAsync(request, Limit, ExportLogsServiceRequest.Parser, x =>
        {
            received = x;
            return new ExportLogsServiceResponse();
        });

        // Assert
        Assert.IsType<FileContentHttpResult>(result);
        Assert.Equal(2, received?.ResourceLogs[0].ScopeLogs[0].LogRecords.Count);
    }

    // protobuf 以外 (JSON) は 415、上限を超えたら 413、壊れた本文は 400
    [Theory]
    [InlineData("application/json", Limit, new byte[] { 0x7b, 0x7d }, StatusCodes.Status415UnsupportedMediaType)]
    [InlineData(OtlpHttpEndpoints.ProtobufContentType, 1, new byte[] { 0x0a, 0x00 }, StatusCodes.Status413PayloadTooLarge)]
    [InlineData(OtlpHttpEndpoints.ProtobufContentType, Limit, new byte[] { 0xff, 0xff, 0xff }, StatusCodes.Status400BadRequest)]
    public async Task ReceiveRejectsInvalidRequest(string contentType, int limit, byte[] body, int statusCode)
    {
        // Arrange
        var request = CreateRequest(body, contentType);

        // Act
        var result = await OtlpHttpEndpoints.ReceiveAsync(request, limit, ExportLogsServiceRequest.Parser, static _ => new ExportLogsServiceResponse());

        // Assert
        Assert.Equal(statusCode, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    }

    private static HttpRequest CreateRequest(byte[] body, string contentType = OtlpHttpEndpoints.ProtobufContentType)
    {
        var context = new DefaultHttpContext { Request = { Method = HttpMethods.Post, ContentType = contentType, Body = new MemoryStream(body) } };
        return context.Request;
    }

    private static ExportLogsServiceRequest CreateMessage() =>
        new()
        {
            ResourceLogs = { new ResourceLogs { ScopeLogs = { new ScopeLogs { LogRecords = { new LogRecord(), new LogRecord() } } } } }
        };
}
