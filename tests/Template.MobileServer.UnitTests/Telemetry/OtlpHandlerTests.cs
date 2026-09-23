namespace Template.MobileServer.Telemetry;

using Grpc.Core;

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

using Template.MobileServer.Web.Telemetry;

public sealed class OtlpHandlerTests
{
    // トレース: リソースごとに件数をログに出し、空の応答 (partial_success なし) を返す。Resource / Scope / Status の無い項目も受け付ける
    [Fact]
    public async Task TraceExportLogsSpanCountPerResource()
    {
        // Arrange
        var logger = CreateLogger<OtlpTraceHandler>();
        var handler = new OtlpTraceHandler(logger);
        var request = new ExportTraceServiceRequest
        {
            ResourceSpans =
            {
                new ResourceSpans
                {
                    Resource = CreateResource(),
                    ScopeSpans = { new ScopeSpans { Scope = new InstrumentationScope { Name = "Template.MobileApp" }, Spans = { new Span { Name = "navigate" }, new Span { Name = "network" } } } }
                },
                new ResourceSpans { ScopeSpans = { new ScopeSpans { Spans = { new Span { Name = "anonymous" } } } } }
            }
        };

        // Act
        var response = await handler.Export(request, Substitute.For<ServerCallContext>());

        // Assert
        Assert.Null(response.PartialSuccess);
        var messages = GetMessages(logger);
        Assert.Contains("Traces received. service=[Template.MobileApp], device=[device-1], spans=[2]", messages);
        Assert.Contains("Traces received. service=[], device=[], spans=[1]", messages);
    }

    // メトリクス: 計器の数と点の数 (ヒストグラムの点を含む)
    [Fact]
    public async Task MetricsExportLogsMetricAndPointCount()
    {
        // Arrange
        var logger = CreateLogger<OtlpMetricsHandler>();
        var handler = new OtlpMetricsHandler(logger);
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
                                new Metric { Name = "process.memory.usage", Gauge = new Gauge { DataPoints = { new NumberDataPoint { AsInt = 1024 } } } },
                                new Metric { Name = "template.network.operation.duration", Histogram = new Histogram { DataPoints = { new HistogramDataPoint { Count = 1 }, new HistogramDataPoint { Count = 2 } } } }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var response = await handler.Export(request, Substitute.For<ServerCallContext>());

        // Assert
        Assert.Null(response.PartialSuccess);
        Assert.Contains("Metrics received. service=[Template.MobileApp], device=[device-1], metrics=[2], points=[3]", GetMessages(logger));
    }

    // ログ: 件数と、各レコードの重大度・本文
    [Fact]
    public async Task LogsExportLogsRecordCountAndContent()
    {
        // Arrange
        var logger = CreateLogger<OtlpLogsHandler>();
        var handler = new OtlpLogsHandler(logger);
        var request = new ExportLogsServiceRequest
        {
            ResourceLogs =
            {
                new ResourceLogs
                {
                    Resource = CreateResource(),
                    ScopeLogs = { new ScopeLogs { LogRecords = { new LogRecord { SeverityNumber = SeverityNumber.Error, Body = new AnyValue { StringValue = "failed" } }, new LogRecord() } } }
                }
            }
        };

        // Act
        var response = await handler.Export(request, Substitute.For<ServerCallContext>());

        // Assert
        Assert.Null(response.PartialSuccess);
        var messages = GetMessages(logger);
        Assert.Contains("Logs received. service=[Template.MobileApp], device=[device-1], records=[2]", messages);
        Assert.Contains(messages, static x => x.StartsWith("Log record.", StringComparison.Ordinal) && x.Contains("severity=[Error], event=[], body=[failed]", StringComparison.Ordinal));
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
                new KeyValue { Key = "device.id", Value = new AnyValue { StringValue = "device-1" } }
            }
        };
}
