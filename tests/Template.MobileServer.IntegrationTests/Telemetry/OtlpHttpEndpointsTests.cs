namespace Template.MobileServer.Telemetry;

using System.Net.Http.Headers;

using Google.Protobuf;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;

using Template.MobileServer.Web.Telemetry;

public sealed class OtlpHttpEndpointsTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public OtlpHttpEndpointsTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // device.id の付いたログは保存する (拒否なし)
    [Fact]
    public async Task LogsWithDeviceIdAreAccepted()
    {
        // Arrange
        using var client = Fixture.CreateOtelHttpClient();
        using var content = CreateContent(CreateLogsRequest($"it-{Guid.NewGuid():N}"));

        // Act
        using var response = await client.PostAsync(new Uri("v1/logs", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reply = ExportLogsServiceResponse.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Null(reply.PartialSuccess);
    }

    // device.id の無いログは拒否した件数を返す
    [Fact]
    public async Task LogsWithoutDeviceIdAreRejected()
    {
        // Arrange
        using var client = Fixture.CreateOtelHttpClient();
        using var content = CreateContent(CreateLogsRequest(null));

        // Act
        using var response = await client.PostAsync(new Uri("v1/logs", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reply = ExportLogsServiceResponse.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(reply.PartialSuccess);
        Assert.Equal(1, reply.PartialSuccess.RejectedLogRecords);
    }

    // OTLP/HTTP の受信口は専用のポートだけで受ける
    [Fact]
    public async Task LogsAreNotFoundOnHttpPort()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        using var content = CreateContent(CreateLogsRequest($"it-{Guid.NewGuid():N}"));

        // Act
        using var response = await client.PostAsync(new Uri("v1/logs", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static ExportLogsServiceRequest CreateLogsRequest(string? deviceId)
    {
        var resource = new Resource();
        resource.Attributes.Add(new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "Template.MobileApp" } });
        if (deviceId is not null)
        {
            resource.Attributes.Add(new KeyValue { Key = "device.id", Value = new AnyValue { StringValue = deviceId } });
        }

        var scopeLogs = new ScopeLogs();
        scopeLogs.LogRecords.Add(new LogRecord
        {
            TimeUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            SeverityNumber = SeverityNumber.Info,
            SeverityText = "Information",
            Body = new AnyValue { StringValue = "message" }
        });
        var resourceLogs = new ResourceLogs { Resource = resource };
        resourceLogs.ScopeLogs.Add(scopeLogs);
        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resourceLogs);
        return request;
    }

    private static ByteArrayContent CreateContent(IMessage message)
    {
        var content = new ByteArrayContent(message.ToByteArray());
        content.Headers.ContentType = new MediaTypeHeaderValue(OtlpHttpEndpoints.ProtobufContentType);
        return content;
    }
}
