namespace Template.MobileServer.Telemetry;

using Google.Protobuf;

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

using Template.MobileServer.Models.Enums;
using Template.MobileServer.Web.Telemetry;

public sealed class OtlpMapperTests
{
    // Resource: 端末の情報を読み、属性の順番が違っても同じハッシュになる (属性はキー順の JSON)
    [Fact]
    public void CreateBatchReadsDeviceInfoAndHashesResource()
    {
        // Arrange
        var resource1 = CreateResource(("service.name", "Template.MobileApp"), ("service.version", "1.0"), ("device.model.identifier", "Pixel 9a"), ("os.version", "16"), ("app.installation.id", "installation-1"));
        var resource2 = CreateResource(("os.version", "16"), ("device.model.identifier", "Pixel 9a"), ("app.installation.id", "installation-1"), ("service.version", "1.0"), ("service.name", "Template.MobileApp"));

        // Act
        var batch1 = OtlpMapper.CreateBatch("device-1", resource1);
        var batch2 = OtlpMapper.CreateBatch("device-1", resource2);

        // Assert
        Assert.Equal("Pixel 9a", batch1.DeviceInfo.Model);
        Assert.Equal("16", batch1.DeviceInfo.OsVersion);
        Assert.Equal("installation-1", batch1.DeviceInfo.InstallationId);
        Assert.Equal("Template.MobileApp", batch1.DeviceInfo.ServiceName);
        Assert.Equal("1.0", batch1.Resource.ServiceVersion);
        Assert.Equal(batch1.Resource.Hash, batch2.Resource.Hash);
        Assert.StartsWith("{\"app.installation.id\":\"installation-1\",\"device.model.identifier\"", batch1.Resource.AttributesJson, StringComparison.Ordinal);
    }

    // スパン: ID は小文字の 16 進、状態・属性 (型を残す)・イベントを変換し、ID の長さが違うスパンは保存しない
    [Fact]
    public void AddSpansConvertsAndRejectsInvalidIds()
    {
        // Arrange
        var batch = OtlpMapper.CreateBatch("device-1", null);
        var span = new Span
        {
            TraceId = CreateId(16, 0xab),
            SpanId = CreateId(8, 0x01),
            Name = "Navigate",
            Kind = Span.Types.SpanKind.Internal,
            StartTimeUnixNano = 1_000,
            EndTimeUnixNano = 3_000,
            Status = new Status { Code = Status.Types.StatusCode.Error, Message = "failed" },
            Attributes = { new KeyValue { Key = "count", Value = new AnyValue { IntValue = 2 } } },
            Events = { new Span.Types.Event { TimeUnixNano = 2_000, Name = "exception" } }
        };
        var invalid = new Span { TraceId = CreateId(4, 0x01), SpanId = CreateId(8, 0x02), Name = "invalid" };

        // Act
        var rejected = OtlpMapper.AddSpans(batch, [new ScopeSpans { Scope = new InstrumentationScope { Name = "Template.MobileApp" }, Spans = { span, invalid } }]);

        // Assert
        Assert.Equal(1, rejected);
        var entity = Assert.Single(batch.Spans);
        Assert.Equal(String.Concat(Enumerable.Repeat("ab", 16)), entity.TraceId);
        Assert.Equal("0101010101010101", entity.SpanId);
        Assert.Equal(string.Empty, entity.ParentSpanId);
        Assert.Equal(TelemetrySpanKind.Internal, entity.Kind);
        Assert.Equal(TelemetryStatusCode.Error, entity.StatusCode);
        Assert.Equal("failed", entity.StatusMessage);
        Assert.Equal("Template.MobileApp", entity.ScopeName);
        Assert.Equal("{\"count\":2}", entity.AttributesJson);
        Assert.Equal("[{\"time\":2000,\"name\":\"exception\",\"attributes\":{}}]", entity.EventsJson);
        Assert.Null(entity.LinksJson);
    }

    // メトリクス: 属性の組み合わせごとに系列を分け、値を記録していない点は数えずに捨て、時刻の無い点は保存しない
    [Fact]
    public void AddMetricsGroupsPointsBySeries()
    {
        // Arrange
        var batch = OtlpMapper.CreateBatch("device-1", null);
        var metrics = new ScopeMetrics
        {
            Scope = new InstrumentationScope { Name = "System.Runtime" },
            Metrics =
            {
                new Metric
                {
                    Name = "dotnet.gc.collections",
                    Unit = "{collection}",
                    Sum = new Sum
                    {
                        AggregationTemporality = AggregationTemporality.Delta,
                        IsMonotonic = true,
                        DataPoints =
                        {
                            new NumberDataPoint { TimeUnixNano = 1_000, AsInt = 3, Attributes = { CreateAttribute("gc.heap.generation", "gen0") } },
                            new NumberDataPoint { TimeUnixNano = 1_000, AsInt = 1, Attributes = { CreateAttribute("gc.heap.generation", "gen1") } },
                            new NumberDataPoint { TimeUnixNano = 2_000, AsInt = 2, Attributes = { CreateAttribute("gc.heap.generation", "gen0") } },
                            new NumberDataPoint { TimeUnixNano = 3_000, Flags = 1 },
                            new NumberDataPoint { AsInt = 5 }
                        }
                    }
                },
                new Metric
                {
                    Name = "http.client.request.duration",
                    Unit = "s",
                    Histogram = new Histogram
                    {
                        AggregationTemporality = AggregationTemporality.Delta,
                        DataPoints = { new HistogramDataPoint { TimeUnixNano = 1_000, Count = 2, Sum = 0.5, Min = 0.1, Max = 0.4, ExplicitBounds = { 0.1, 1 }, BucketCounts = { 0, 2, 0 } } }
                    }
                }
            }
        };

        // Act
        var rejected = OtlpMapper.AddMetrics(batch, [metrics]);

        // Assert
        Assert.Equal(1, rejected);
        Assert.Equal(3, batch.Metrics.Count);
        var gen0 = batch.Metrics.Single(static x => x.Series.AttributesJson == "{\"gc.heap.generation\":\"gen0\"}");
        Assert.Equal(TelemetryMetricKind.Sum, gen0.Series.Kind);
        Assert.Equal(TelemetryTemporality.Delta, gen0.Series.Temporality);
        Assert.True(gen0.Series.IsMonotonic);
        Assert.Equal("System.Runtime", gen0.Series.ScopeName);
        Assert.Equal([3, 2], gen0.Points.Select(static x => x.Value ?? 0));
        var histogram = batch.Metrics.Single(static x => x.Series.Kind == TelemetryMetricKind.Histogram);
        var point = Assert.Single(histogram.Points);
        Assert.Equal(2, point.Count);
        Assert.Equal(0.5, point.Sum);
        Assert.Equal(0.4, point.Max);
        Assert.Equal("{\"bounds\":[0.1,1],\"counts\":[0,2,0]}", point.Detail);
    }

    // ログ: 時刻が無ければ観測時刻、文字列以外の本文は JSON、同じ内容は同じハッシュ。時刻の無いログは保存しない
    [Fact]
    public void AddLogsConvertsRecords()
    {
        // Arrange
        var batch = OtlpMapper.CreateBatch("device-1", null);
        var record = new LogRecord
        {
            ObservedTimeUnixNano = 5_000,
            SeverityNumber = SeverityNumber.Error,
            SeverityText = "Error",
            EventName = "exception",
            Body = new AnyValue { KvlistValue = new KeyValueList { Values = { CreateAttribute("message", "failed") } } },
            TraceId = CreateId(16, 0x0f),
            SpanId = CreateId(8, 0x0e)
        };
        var other = new LogRecord { TimeUnixNano = 6_000, Body = new AnyValue { StringValue = "other" } };
        var invalid = new LogRecord { Body = new AnyValue { StringValue = "no time" } };

        // Act
        var rejected = OtlpMapper.AddLogs(batch, [new ScopeLogs { LogRecords = { record, record.Clone(), other, invalid } }]);

        // Assert
        Assert.Equal(1, rejected);
        Assert.Equal(3, batch.Logs.Count);
        var log = batch.Logs[0];
        Assert.Equal(5_000, log.TimeUnixNano);
        Assert.Equal(17, log.SeverityNumber);
        Assert.Equal("exception", log.EventName);
        Assert.Equal("{\"message\":\"failed\"}", log.Body);
        Assert.Equal(String.Concat(Enumerable.Repeat("0f", 16)), log.TraceId);
        Assert.Equal(log.Hash, batch.Logs[1].Hash);
        Assert.NotEqual(log.Hash, batch.Logs[2].Hash);
        Assert.Equal("other", batch.Logs[2].Body);
    }

    private static Resource CreateResource(params (string Key, string Value)[] attributes)
    {
        var resource = new Resource();
        resource.Attributes.AddRange(attributes.Select(static x => CreateAttribute(x.Key, x.Value)));
        return resource;
    }

    private static KeyValue CreateAttribute(string key, string value) =>
        new() { Key = key, Value = new AnyValue { StringValue = value } };

    private static ByteString CreateId(int length, byte value) =>
        ByteString.CopyFrom(Enumerable.Repeat(value, length).ToArray());
}
