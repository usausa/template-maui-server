namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Template.MobileServer.Domain;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Components.Pages;

public sealed class TelemetryPageTests : MudBlazorTestBase
{
    private const string DeviceId = "device-1";

    private static readonly string TraceId = new('a', 32);

    // 表示: 端末の見出しと、既知の計器のグラフ (名前と単位を整え、凡例に最新値)
    [Fact]
    public async Task RenderShowsHeaderAndCharts()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);

        // Act
        var cut = Render<TelemetryPage>(parameters => parameters.Add(static x => x.DeviceId, DeviceId));

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Equal(["CPU", "メモリ"], cut.FindAll(".chart-title").Select(static x => x.TextContent)));
        Assert.Contains("Pixel 9a", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("20%", cut.Find(".chart-legend").TextContent, StringComparison.Ordinal);
    }

    // 未登録の端末は見出しとグラフを出さない
    [Fact]
    public async Task UnknownDeviceShowsMessage()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);

        // Act
        var cut = Render<TelemetryPage>(parameters => parameters.Add(static x => x.DeviceId, "unknown"));

        // Assert
        Assert.Contains("端末が見つかりません。", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".chart"));
    }

    // 通知: 表示中の端末の受信をグラフに足す
    [Fact]
    public async Task ReceivedPointsAreAdded()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);
        var cut = Render<TelemetryPage>(parameters => parameters.Add(static x => x.DeviceId, DeviceId));
        await cut.WaitForAssertionAsync(() => Assert.Contains("20%", cut.Find(".chart-legend").TextContent, StringComparison.Ordinal));

        // Act
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            var result = await storage.Service.SaveAsync(CreateBatch(0.5), Xunit.TestContext.Current.CancellationToken);
            storage.Registry.Apply(result);
            storage.Bus.PublishReceived(result);
        }

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Contains("50%", cut.Find(".chart-legend").TextContent, StringComparison.Ordinal), TimeSpan.FromSeconds(5));
    }

    // トレース: タブと選んだトレースを URL のクエリで開き、一覧・ウォーターフォール (親子)・ルートスパンの詳細を出す
    [Fact]
    public async Task TracesTabShowsWaterfall()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);

        // Act
        Services.GetRequiredService<NavigationManager>().NavigateTo($"telemetry/{DeviceId}?tab=traces&trace={TraceId}");
        var cut = Render<TelemetryPage>(parameters => parameters.Add(static x => x.DeviceId, DeviceId));

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Equal(2, cut.FindAll(".waterfall-row:not(.waterfall-head)").Count));
        Assert.Contains("TelemetryTest", Assert.Single(cut.FindAll("tr.trace-row")).TextContent, StringComparison.Ordinal);
        Assert.Contains("TelemetryTest", cut.Find(".span-detail-name").TextContent, StringComparison.Ordinal);
    }

    // ログ: 重大度 (URL の level) で絞り、行を開くと例外のスタックトレース。受信したログは先頭に足す
    [Fact]
    public async Task LogsTabFiltersAndExpands()
    {
        // Arrange
        using var storage = new TelemetryTestStorage();
        await PrepareAsync(storage);

        // Act
        Services.GetRequiredService<NavigationManager>().NavigateTo($"telemetry/{DeviceId}?tab=logs&level=error");
        var cut = Render<TelemetryPage>(parameters => parameters.Add(static x => x.DeviceId, DeviceId));
        await cut.WaitForAssertionAsync(() => Assert.Single(cut.FindAll("tr.log-row")));
        await cut.Find("tr.log-row").ClickAsync(new MouseEventArgs());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Contains("at Main", cut.Find(".stack-trace").TextContent, StringComparison.Ordinal));

        // Act: 受信
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            var batch = CreateBatch(0.3);
            batch.Logs.Add(CreateLog(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000, TelemetrySeverity.Fatal, "crashed", "{}"));
            var result = await storage.Service.SaveAsync(batch, Xunit.TestContext.Current.CancellationToken);
            storage.Bus.PublishReceived(result);
        }

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Contains("crashed", cut.FindAll("tr.log-row")[0].TextContent, StringComparison.Ordinal), TimeSpan.FromSeconds(5));
    }

    // CPU 20% とメモリの点、親子のスパン、警告とエラーのログを保存した端末
    private async Task PrepareAsync(TelemetryTestStorage storage)
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        await storage.PrepareDatabaseAsync();
        using (storage.BeginScope(DateTimeOffset.Now))
        {
            var batch = CreateBatch(0.2);
            batch.Logs.Add(CreateLog(now - 2_000_000, TelemetrySeverity.Warn, "warning", "{}"));
            batch.Logs.Add(CreateLog(now - 1_000_000, TelemetrySeverity.Error, "failed", "{\"exception.stacktrace\":\"System.Exception\\n   at Main\"}"));
            await storage.Registry.EnsureRegisteredAsync(batch.DeviceInfo, cancellationToken);
            storage.Registry.Apply(await storage.Service.SaveAsync(batch, cancellationToken));
        }

        Services.AddSingleton(storage.Registry);
        Services.AddSingleton(storage.Service);
        Services.AddSingleton(storage.Bus);
    }

    // 時刻は今
    private static TelemetryBatch CreateBatch(double cpu)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
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
            Resource = new TelemetryResourceEntity { Hash = "hash-1", ServiceInstanceId = "instance-1", ServiceVersion = "1.0", AttributesJson = "{}" }
        };

        batch.Metrics.Add(CreateMetric("process.cpu.utilization", TelemetryMetricKind.Gauge, "1", now, cpu));
        batch.Metrics.Add(CreateMetric("process.memory.usage", TelemetryMetricKind.Sum, "By", now, 100 * 1024 * 1024));
        batch.Spans.Add(CreateSpan("0000000000000001", string.Empty, "TelemetryTest", now - 1_000_000, now));
        batch.Spans.Add(CreateSpan("0000000000000002", "0000000000000001", "Compute", now - 900_000, now - 100_000));
        return batch;
    }

    private static TelemetryLogEntity CreateLog(long time, int severity, string body, string attributesJson) =>
        new()
        {
            TimeUnixNano = time,
            ObservedTimeUnixNano = time,
            SeverityNumber = severity,
            SeverityText = string.Empty,
            EventName = string.Empty,
            Body = body,
            TraceId = string.Empty,
            SpanId = string.Empty,
            ScopeName = "Template.MobileApp",
            AttributesJson = attributesJson,
            Hash = time
        };

    private static TelemetrySpanEntity CreateSpan(string spanId, string parentSpanId, string name, long start, long end) =>
        new()
        {
            TraceId = TraceId,
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            Name = name,
            Kind = TelemetrySpanKind.Internal,
            StartTimeUnixNano = start,
            EndTimeUnixNano = end,
            StatusCode = TelemetryStatusCode.Unset,
            StatusMessage = string.Empty,
            ScopeName = "Template.MobileApp",
            AttributesJson = "{}"
        };

    private static TelemetryMetric CreateMetric(string name, TelemetryMetricKind kind, string unit, long time, double value)
    {
        var metric = new TelemetryMetric
        {
            Series = new TelemetryMetricSeriesEntity
            {
                Name = name,
                ScopeName = "Template.MobileApp",
                Unit = unit,
                Kind = kind,
                Temporality = kind == TelemetryMetricKind.Sum ? TelemetryTemporality.Cumulative : TelemetryTemporality.Unspecified,
                AttributesJson = "{}"
            }
        };
        metric.Points.Add(new TelemetryMetricPointEntity { TimeUnixNano = time, StartTimeUnixNano = time, Value = value });
        return metric;
    }
}
