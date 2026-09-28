namespace Template.MobileServer.Services;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Telemetry;

public sealed class TelemetryServiceTests : IDisposable
{
    private const string DeviceId = "device-1";

    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.FromHours(9));

    private static readonly string TraceId = new('a', 32);

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 保存: 点・スパン・ログを端末のファイルに保存し、同じ内容の送り直しは入れない。列挙型は名前の文字列、時刻は Unix ナノ秒
    [Fact]
    public async Task SaveStoresItemsAndIgnoresDuplicates()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);

        // Act
        var first = await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);
        var second = await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, Assert.Single(first.Metrics).Points.Count);
        Assert.Single(first.Spans);
        Assert.Single(first.Traces);
        Assert.Single(first.Logs);
        Assert.Empty(second.Metrics);
        Assert.Empty(second.Spans);
        Assert.Empty(second.Logs);
        Assert.Equal(2L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM MetricPoints"));
        Assert.Equal(1L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM Logs"));
        Assert.Equal(1L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM Resources"));
        Assert.Equal("Internal", await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT Kind FROM Spans"));
        Assert.Equal("Gauge", await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT Kind FROM MetricSeries"));
        Assert.Equal(Now.ToUnixTimeMilliseconds() * 1_000_000, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT LastReceivedAt FROM DeviceInfo"));
    }

    // 系列: 同じ系列は次の保存でも同じ Id を使い、新しい時刻の点だけが入る
    [Fact]
    public async Task SaveReusesSeries()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        var first = await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);
        var batch = CreateBatch();
        batch.Metrics[0].Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = 3_000, Value = 300 });

        // Act
        var second = await storage.Service.SaveAsync(batch, TestContext.Current.CancellationToken);

        // Assert
        var metric = Assert.Single(second.Metrics);
        Assert.Equal(first.Metrics[0].Series.Id, metric.Series.Id);
        Assert.Equal(3_000, Assert.Single(metric.Points).TimeUnixNano);
        Assert.Equal(1L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM MetricSeries"));
    }

    // トレース: 子のスパンが先に届くと最初のスパンの名前、ルートが届くとルートの名前で集計し直す
    [Fact]
    public async Task SaveRecalculatesTrace()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        var child = CreateBatch([CreateSpan("0000000000000002", "0000000000000001", "Compute", 2_000, 3_000, TelemetryStatusCode.Error)]);
        var root = CreateBatch([CreateSpan("0000000000000001", string.Empty, "TelemetryTest", 1_000, 4_000, TelemetryStatusCode.Unset)]);

        // Act / Assert: 子だけ
        var first = await storage.Service.SaveAsync(child, TestContext.Current.CancellationToken);
        var trace = Assert.Single(first.Traces);
        Assert.Equal("Compute", trace.RootName);
        Assert.Equal(1, trace.ErrorCount);

        // Act / Assert: ルートが届く
        var second = await storage.Service.SaveAsync(root, TestContext.Current.CancellationToken);
        trace = Assert.Single(second.Traces);
        Assert.Equal("TelemetryTest", trace.RootName);
        Assert.Equal(2, trace.SpanCount);
        Assert.Equal(1, trace.ErrorCount);
        Assert.Equal(1_000, trace.StartTimeUnixNano);
        Assert.Equal(4_000, trace.EndTimeUnixNano);
    }

    // 保持期間: 種類ごとの時刻より前の行を削除する (スパンはトレースの開始で判定)
    [Fact]
    public async Task DeleteExpiredRemovesOldRows()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);

        // Act
        var result = await storage.Service.DeleteExpiredAsync(DeviceId, 1_600, 1_500, 1_500, 0, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.FileDeleted);
        Assert.Equal(4, result.Rows);
        Assert.Equal(0L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM Logs"));
        Assert.Equal(0L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM Spans"));
        Assert.Equal(0L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT COUNT(*) FROM Traces"));
        Assert.Equal(2_000L, await storage.QueryValueAsync(DeviceId, static x => x.CommandText = "SELECT TimeUnixNano FROM MetricPoints"));
    }

    // 保持期間: 最後の受信が時刻より前の端末は、ファイルごと削除する
    [Fact]
    public async Task DeleteExpiredDeletesFileOfSilentDevice()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        var saved = await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);

        // Act
        var result = await storage.Service.DeleteExpiredAsync(DeviceId, 0, 0, 0, saved.ReceivedAt + 1, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.FileDeleted);
        Assert.False(File.Exists(Path.Combine(storage.Root, DeviceId + ".db")));
    }

    // 推移: 時刻以降の点を系列ごと・間隔ごとに束ねる (1 なら点ごと)。ファイルが無ければ空
    [Fact]
    public async Task QueryMetricHistoryGroupsPoints()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        await storage.Service.SaveAsync(CreateBatch(), TestContext.Current.CancellationToken);

        // Act
        var grouped = await storage.Service.QueryMetricHistoryAsync(DeviceId, 0, 10_000, TestContext.Current.CancellationToken);
        var raw = await storage.Service.QueryMetricHistoryAsync(DeviceId, 1_500, 1, TestContext.Current.CancellationToken);
        var missing = await storage.Service.QueryMetricHistoryAsync("device-2", 0, 1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(grouped.Series);
        var bucket = Assert.Single(grouped.Buckets);
        Assert.Equal(0, bucket.BucketUnixNano);
        Assert.Equal(2, bucket.ValueCount);
        Assert.Equal(300, bucket.TotalValue);
        Assert.Equal(3_000, bucket.Duration);
        Assert.Equal(2_000, bucket.LastUnixNano);
        Assert.Equal(2_000, Assert.Single(raw.Buckets).BucketUnixNano);
        Assert.Empty(missing.Series);
        Assert.Empty(missing.Buckets);
    }

    // トレース: エラーのあるものだけ・ルートスパンの名前の部分一致 (% は文字として扱う) で絞る。詳細はスパン (開始の順)・ログ・Resource
    [Fact]
    public async Task QueryTracesFiltersAndReturnsDetail()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        await storage.Service.SaveAsync(
            CreateBatch(
            [
                CreateSpan("0000000000000002", "0000000000000001", "Load", 1_200, 1_800, TelemetryStatusCode.Error),
                CreateSpan("0000000000000001", string.Empty, "Navigate", 1_000, 2_000, TelemetryStatusCode.Unset)
            ]),
            TestContext.Current.CancellationToken);

        // Act
        var all = await storage.Service.QueryTraceListAsync(DeviceId, 0, false, null, 10, TestContext.Current.CancellationToken);
        var errors = await storage.Service.QueryTraceListAsync(DeviceId, 0, true, null, 10, TestContext.Current.CancellationToken);
        var named = await storage.Service.QueryTraceListAsync(DeviceId, 0, false, "navi", 10, TestContext.Current.CancellationToken);
        var escaped = await storage.Service.QueryTraceListAsync(DeviceId, 0, false, "%", 10, TestContext.Current.CancellationToken);
        var later = await storage.Service.QueryTraceListAsync(DeviceId, 1_001, false, null, 10, TestContext.Current.CancellationToken);
        var detail = await storage.Service.QueryTraceDetailAsync(DeviceId, TraceId, TestContext.Current.CancellationToken);
        var missing = await storage.Service.QueryTraceDetailAsync(DeviceId, new string('b', 32), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Navigate", Assert.Single(all).RootName);
        Assert.Single(errors);
        Assert.Single(named);
        Assert.Empty(escaped);
        Assert.Empty(later);
        Assert.NotNull(detail);
        Assert.Equal(["Navigate", "Load"], detail.Spans.Select(static x => x.Name));
        Assert.Empty(detail.Logs);
        Assert.Single(detail.Resources);
        Assert.Null(missing);
    }

    // ログ: 重大度以上・本文の部分一致 (大文字と小文字を区別しない)・トレースで絞り、新しい順に件数ずつ (続きは時刻と Id の前から)。Resource は Id で
    [Fact]
    public async Task QueryLogListFiltersAndPages()
    {
        // Arrange
        using var scope = storage.BeginScope(Now);
        var batch = CreateBatch();
        batch.Logs.Add(CreateLog(2_500, 17, "error one", TraceId));
        batch.Logs.Add(CreateLog(3_500, 21, "crash", string.Empty));
        await storage.Service.SaveAsync(batch, TestContext.Current.CancellationToken);

        // Act
        var all = await storage.Service.QueryLogListAsync(DeviceId, new TelemetryLogQuery { Limit = 10 }, TestContext.Current.CancellationToken);
        var errors = await storage.Service.QueryLogListAsync(DeviceId, new TelemetryLogQuery { Severity = 17, Limit = 10 }, TestContext.Current.CancellationToken);
        var body = await storage.Service.QueryLogListAsync(DeviceId, new TelemetryLogQuery { Body = "ONE", Limit = 10 }, TestContext.Current.CancellationToken);
        var trace = await storage.Service.QueryLogListAsync(DeviceId, new TelemetryLogQuery { TraceId = TraceId, Limit = 10 }, TestContext.Current.CancellationToken);
        var first = await storage.Service.QueryLogListAsync(DeviceId, new TelemetryLogQuery { Limit = 2 }, TestContext.Current.CancellationToken);
        var next = await storage.Service.QueryLogListAsync(DeviceId, new TelemetryLogQuery { BeforeTime = first[^1].TimeUnixNano, BeforeId = first[^1].Id, Limit = 2 }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["crash", "error one", "warning"], all.Select(static x => x.Body));
        Assert.Equal(2, errors.Count);
        Assert.Equal("error one", Assert.Single(body).Body);
        Assert.Equal("error one", Assert.Single(trace).Body);
        Assert.Equal(["crash", "error one"], first.Select(static x => x.Body));
        Assert.Equal("warning", Assert.Single(next).Body);
        Assert.NotNull(await storage.Service.QueryResourceAsync(DeviceId, all[0].ResourceId, TestContext.Current.CancellationToken));
    }

    private static TelemetryLogEntity CreateLog(long time, int severity, string body, string traceId) =>
        new()
        {
            TimeUnixNano = time,
            ObservedTimeUnixNano = time,
            SeverityNumber = severity,
            SeverityText = string.Empty,
            EventName = string.Empty,
            Body = body,
            TraceId = traceId,
            SpanId = string.Empty,
            ScopeName = "Template.MobileApp",
            AttributesJson = "{}",
            Hash = time
        };

    private static TelemetryBatch CreateBatch(IEnumerable<TelemetrySpanEntity>? spans = null)
    {
        var batch = new TelemetryBatch
        {
            DeviceId = DeviceId,
            DeviceInfo = new TelemetryDeviceInfoEntity
            {
                DeviceId = DeviceId,
                InstallationId = "installation-1",
                Manufacturer = "Google",
                Model = "Pixel 9a",
                OsName = "Android",
                OsVersion = "16",
                ServiceName = "Template.MobileApp",
                ServiceVersion = "1.0"
            },
            Resource = new TelemetryResourceEntity
            {
                Hash = "hash-1",
                ServiceInstanceId = "instance-1",
                ServiceVersion = "1.0",
                AttributesJson = "{}"
            }
        };

        var metric = new TelemetryMetric
        {
            Series = new TelemetryMetricSeriesEntity
            {
                Name = "process.memory.usage",
                ScopeName = "Template.MobileApp",
                Unit = "By",
                Kind = TelemetryMetricKind.Gauge,
                AttributesJson = "{}"
            }
        };
        metric.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = 1_000, Value = 100 });
        metric.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = 2_000, Value = 200 });
        batch.Metrics.Add(metric);

        foreach (var span in spans ?? [CreateSpan("0000000000000001", string.Empty, "Navigate", 1_000, 2_000, TelemetryStatusCode.Unset)])
        {
            batch.Spans.Add(span);
        }

        batch.Logs.Add(new TelemetryLogEntity
        {
            TimeUnixNano = 1_500,
            ObservedTimeUnixNano = 1_500,
            SeverityNumber = 13,
            SeverityText = "Warning",
            EventName = string.Empty,
            Body = "warning",
            TraceId = string.Empty,
            SpanId = string.Empty,
            ScopeName = "Template.MobileApp",
            AttributesJson = "{}",
            Hash = 1
        });

        return batch;
    }

    private static TelemetrySpanEntity CreateSpan(string spanId, string parentSpanId, string name, long start, long end, TelemetryStatusCode statusCode) =>
        new()
        {
            TraceId = TraceId,
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            Name = name,
            Kind = TelemetrySpanKind.Internal,
            StartTimeUnixNano = start,
            EndTimeUnixNano = end,
            StatusCode = statusCode,
            StatusMessage = string.Empty,
            ScopeName = "Template.MobileApp",
            AttributesJson = "{}"
        };
}
