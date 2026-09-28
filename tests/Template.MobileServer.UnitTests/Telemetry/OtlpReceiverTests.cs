namespace Template.MobileServer.Telemetry;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using NSubstitute;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Services;
using Template.MobileServer.Web.Telemetry;

public sealed class OtlpReceiverTests : IDisposable
{
    private const string DeviceId = "device-1";

    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // トレース: 端末ごとに保存し、端末を識別できないリソースのスパンは partial_success の拒否件数で返す
    [Fact]
    public async Task ReceiveTracesSavesAndRejectsUnknownDevice()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        var logger = CreateLogger<OtlpReceiver>();
        var receiver = new OtlpReceiver(logger, storage.Service, storage.Registry, storage.Bus);
        var request = new ExportTraceServiceRequest
        {
            ResourceSpans =
            {
                new ResourceSpans
                {
                    Resource = CreateResource(),
                    ScopeSpans = { new ScopeSpans { Scope = new InstrumentationScope { Name = "Template.MobileApp" }, Spans = { CreateSpan(1), CreateSpan(2) } } }
                },
                new ResourceSpans { ScopeSpans = { new ScopeSpans { Spans = { CreateSpan(3) } } } }
            }
        };
        using var scope = storage.BeginScope(Now);

        // Act
        var response = await receiver.ReceiveAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, response.PartialSuccess.RejectedSpans);
        Assert.Equal(2L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM Spans"));
        Assert.Contains("Telemetry rejected. service=[], device=[], items=[1]", GetMessages(logger));
    }

    // メトリクス: 時刻の無い点は partial_success の拒否件数に入り、ほかの点は保存する
    [Fact]
    public async Task ReceiveMetricsRejectsPointWithoutTime()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        var receiver = new OtlpReceiver(CreateLogger<OtlpReceiver>(), storage.Service, storage.Registry, storage.Bus);
        var request = new ExportMetricsServiceRequest
        {
            ResourceMetrics =
            {
                new ResourceMetrics
                {
                    Resource = CreateResource(),
                    ScopeMetrics =
                    {
                        new ScopeMetrics
                        {
                            Metrics =
                            {
                                new Metric { Name = "process.memory.usage", Unit = "By", Gauge = new Gauge { DataPoints = { new NumberDataPoint { TimeUnixNano = 1_000, AsInt = 1024 }, new NumberDataPoint { AsInt = 2048 } } } }
                            }
                        }
                    }
                }
            }
        };
        using var scope = storage.BeginScope(Now);

        // Act
        var response = await receiver.ReceiveAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, response.PartialSuccess.RejectedDataPoints);
        Assert.Equal(1024d, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT Value FROM MetricPoints"));
    }

    // ログ: 未登録の端末を登録して保存し、バスに通知する。partial_success は無い
    [Fact]
    public async Task ReceiveLogsSavesRecords()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        var received = new List<TelemetrySaveResult>();
        storage.Bus.Received += (_, e) => received.Add(e.Result);
        var receiver = new OtlpReceiver(CreateLogger<OtlpReceiver>(), storage.Service, storage.Registry, storage.Bus);
        var request = new ExportLogsServiceRequest
        {
            ResourceLogs =
            {
                new ResourceLogs
                {
                    Resource = CreateResource(),
                    ScopeLogs = { new ScopeLogs { LogRecords = { new LogRecord { TimeUnixNano = 1_000, SeverityNumber = SeverityNumber.Warn, Body = new AnyValue { StringValue = "warning" } } } } }
                }
            }
        };
        using var scope = storage.BeginScope(Now);

        // Act
        var response = await receiver.ReceiveAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(response.PartialSuccess);
        Assert.Equal("warning", await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT Body FROM Logs"));
        var device = await storage.DeviceService.QueryAsync(DeviceId, TestContext.Current.CancellationToken);
        Assert.NotNull(device);
        Assert.True(device.IsEnabled);
        Assert.Single(Assert.Single(received).Logs);
    }

    // 無効の端末: 保存せず、partial_success の拒否件数と理由で返す
    [Fact]
    public async Task ReceiveRejectsDisabledDevice()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        await storage.Registry.AddAsync(new DeviceEntity { DeviceId = DeviceId, Name = "disabled", IsEnabled = false }, TestContext.Current.CancellationToken);
        var receiver = new OtlpReceiver(CreateLogger<OtlpReceiver>(), storage.Service, storage.Registry, storage.Bus);
        var request = new ExportLogsServiceRequest
        {
            ResourceLogs =
            {
                new ResourceLogs
                {
                    Resource = CreateResource(),
                    ScopeLogs = { new ScopeLogs { LogRecords = { new LogRecord { TimeUnixNano = 1_000, Body = new AnyValue { StringValue = "ignored" } } } } }
                }
            }
        };

        // Act
        var response = await receiver.ReceiveAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, response.PartialSuccess.RejectedLogRecords);
        Assert.Equal("The device is disabled.", response.PartialSuccess.ErrorMessage);
        Assert.False(File.Exists(Path.Combine(storage.Root, DeviceId + ".db")));
    }

    // 保存の失敗: 例外のまま返す (受信口が 503 / UNAVAILABLE にする)
    [Fact]
    public async Task ReceiveThrowsStorageError()
    {
        // Arrange: the database file cannot be opened
        await storage.PrepareDatabaseAsync();
        Directory.CreateDirectory(Path.Combine(storage.Root, DeviceId + ".db"));
        var logger = CreateLogger<OtlpReceiver>();
        var receiver = new OtlpReceiver(logger, storage.Service, storage.Registry, storage.Bus);
        var request = new ExportTraceServiceRequest
        {
            ResourceSpans = { new ResourceSpans { Resource = CreateResource(), ScopeSpans = { new ScopeSpans { Spans = { CreateSpan(1) } } } } }
        };
        using var scope = storage.BeginScope(Now);

        // Act
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await receiver.ReceiveAsync(request, TestContext.Current.CancellationToken));

        // Assert
        Assert.True(OtlpReceiver.IsStorageError(ex));
        Assert.Contains("Telemetry save failed. device=[device-1]", GetMessages(logger));
    }

    private static ILogger<T> CreateLogger<T>()
    {
        var logger = Substitute.For<ILogger<T>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }

    // LoggerMessage の状態は ToString でメッセージになる
    private static List<string> GetMessages(ILogger logger) =>
        logger.ReceivedCalls()
            .Where(static x => x.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(static x => x.GetArguments()[2]?.ToString() ?? string.Empty)
            .ToList();

    private static Resource CreateResource() =>
        new()
        {
            Attributes =
            {
                new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "Template.MobileApp" } },
                new KeyValue { Key = "device.id", Value = new AnyValue { StringValue = DeviceId } }
            }
        };

    private static Span CreateSpan(byte id) =>
        new()
        {
            TraceId = ByteString.CopyFrom(Enumerable.Repeat((byte)0xab, 16).ToArray()),
            SpanId = ByteString.CopyFrom(Enumerable.Repeat(id, 8).ToArray()),
            Name = "Navigate",
            StartTimeUnixNano = 1_000,
            EndTimeUnixNano = 2_000
        };
}
