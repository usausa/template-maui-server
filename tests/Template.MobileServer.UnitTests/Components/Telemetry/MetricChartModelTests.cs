namespace Template.MobileServer.Components.Telemetry;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Models.Parameters;
using Template.MobileServer.Models.Views;
using Template.MobileServer.Web.Components.Telemetry;

public sealed class MetricChartModelTests
{
    private const long Second = 1_000_000_000;

    private const long Minute = 60 * Second;

    // 読み込み: 既知の計器は名前と単位を整えて先に並べる。値はゲージ = 平均、単調な Delta = 1 分あたり、ヒストグラム = 平均
    [Fact]
    public void LoadBuildsChartsByKind()
    {
        // Arrange
        var model = new MetricChartModel(Minute);
        var history = new TelemetryMetricHistoryView(
            [
                CreateSeries(1, "custom.metric", TelemetryMetricKind.Gauge, "{item}"),
                CreateSeries(2, "process.cpu.utilization", TelemetryMetricKind.Gauge, "1"),
                CreateSeries(3, "dotnet.exceptions", TelemetryMetricKind.Sum, "{exception}", "{\"error.type\":\"IOException\"}"),
                CreateSeries(4, "http.client.request.duration", TelemetryMetricKind.Histogram, "s", "{\"http.request.method\":\"GET\",\"http.response.status_code\":200}")
            ],
            [
                new TelemetryMetricBucketView(1, 0, 1, 1, 5, 0, null, null, null, 10 * Second),
                new TelemetryMetricBucketView(2, 0, 2, 2, 0.5, 0, null, null, null, 40 * Second),
                new TelemetryMetricBucketView(3, 0, 2, 2, 3, 30 * Second, null, null, null, 40 * Second),
                new TelemetryMetricBucketView(4, 0, 2, 0, null, 30 * Second, 4, 0.2, 0.1, 40 * Second)
            ]);

        // Act
        model.Load(history);

        // Assert
        Assert.Equal(["CPU", "例外", "HTTP", "custom.metric"], model.Charts.Select(static x => x.Title));
        var cpu = model.Charts[0];
        Assert.Equal("%", cpu.Unit);
        Assert.Equal(25, cpu.GetValue(cpu.Lines[0].Buckets[0])!.Value, 6);
        var exceptions = model.Charts[1];
        Assert.Equal("件 / 分", exceptions.Unit);
        Assert.Equal("IOException", exceptions.Lines[0].Label);
        Assert.Equal(6, exceptions.GetValue(exceptions.Lines[0].Buckets[0])!.Value, 6);
        var http = model.Charts[2];
        Assert.Equal("GET 200", http.Lines[0].Label);
        Assert.Equal(50, http.GetValue(http.Lines[0].Buckets[0])!.Value, 6);
        Assert.Equal(100, http.GetMax(http.Lines[0].Buckets[0])!.Value, 6);
        var custom = model.Charts[3];
        Assert.Equal("{item}", custom.Unit);
        Assert.Equal(string.Empty, custom.Lines[0].Label);
    }

    // アプリケーション固有値: 「値 番号」にして既知の計器の後に番号の順、ほかの計器はその後
    [Fact]
    public void LoadNamesCustomValuesByNumber()
    {
        // Arrange
        var model = new MetricChartModel(Minute);
        var history = new TelemetryMetricHistoryView(
            [
                CreateSeries(1, "application.custom.value10", TelemetryMetricKind.Gauge, string.Empty),
                CreateSeries(2, "custom.metric", TelemetryMetricKind.Gauge, "{item}"),
                CreateSeries(3, "application.custom.value2", TelemetryMetricKind.Gauge, string.Empty),
                CreateSeries(4, "process.cpu.utilization", TelemetryMetricKind.Gauge, "1")
            ],
            [
                new TelemetryMetricBucketView(1, 0, 1, 1, 10, 0, null, null, null, 10 * Second),
                new TelemetryMetricBucketView(2, 0, 1, 1, 5, 0, null, null, null, 10 * Second),
                new TelemetryMetricBucketView(3, 0, 1, 1, 20, 0, null, null, null, 10 * Second),
                new TelemetryMetricBucketView(4, 0, 1, 1, 0.5, 0, null, null, null, 10 * Second)
            ]);

        // Act
        model.Load(history);

        // Assert
        Assert.Equal(["CPU", "値 2", "値 10", "custom.metric"], model.Charts.Select(static x => x.Title));
        var value2 = model.Charts[1];
        Assert.Equal(string.Empty, value2.Unit);
        Assert.Equal(20, value2.GetValue(value2.Lines[0].Buckets[0])!.Value, 6);
    }

    // 通知: 読み込み済みの時刻より後の点だけを束に足し、新しい系列は線とグラフを足す。範囲より前の点は足さない
    [Fact]
    public void AddMergesNewPoints()
    {
        // Arrange
        var cpu = CreateSeries(1, "process.cpu.utilization", TelemetryMetricKind.Gauge, "1");
        var model = new MetricChartModel(Minute);
        model.Load(new TelemetryMetricHistoryView([cpu], [new TelemetryMetricBucketView(1, 0, 1, 1, 0.2, 0, null, null, null, 10 * Second)]));

        // Act
        var added = model.Add(
            [
                CreateMetric(cpu, 10 * Second, 0.9),
                CreateMetric(cpu, 40 * Second, 0.4),
                CreateMetric(CreateSeries(2, "process.memory.usage", TelemetryMetricKind.Sum, "By"), Minute + Second, 3 * 1024 * 1024)
            ],
            0);
        var skipped = model.Add([CreateMetric(CreateSeries(3, "custom.metric", TelemetryMetricKind.Gauge, "1"), 5 * Second, 1)], Minute);

        // Assert
        Assert.True(added);
        Assert.False(skipped);
        Assert.Equal(["CPU", "メモリ"], model.Charts.Select(static x => x.Title));
        var chart = model.Charts[0];
        Assert.Equal(30, chart.GetValue(chart.Lines[0].Buckets[0])!.Value, 6);
        var memory = model.Charts[1];
        Assert.Equal(3, memory.GetValue(memory.Lines[0].Buckets[Minute])!.Value, 6);
    }

    // 範囲: 範囲の始まりを含む束より前の束を消す
    [Fact]
    public void TrimRemovesBucketsBeforeRange()
    {
        // Arrange
        var model = new MetricChartModel(Minute);
        model.Load(new TelemetryMetricHistoryView(
            [CreateSeries(1, "process.cpu.utilization", TelemetryMetricKind.Gauge, "1")],
            [
                new TelemetryMetricBucketView(1, 0, 1, 1, 0.1, 0, null, null, null, 0),
                new TelemetryMetricBucketView(1, Minute, 1, 1, 0.2, 0, null, null, null, Minute),
                new TelemetryMetricBucketView(1, 2 * Minute, 1, 1, 0.3, 0, null, null, null, 2 * Minute)
            ]));

        // Act
        model.Trim(Minute + Second);

        // Assert
        Assert.Equal([Minute, 2 * Minute], model.Charts[0].Lines[0].Buckets.Keys);
    }

    private static TelemetryMetricSeriesEntity CreateSeries(long id, string name, TelemetryMetricKind kind, string unit, string attributesJson = "{}") =>
        new()
        {
            Id = id,
            Name = name,
            ScopeName = "Template.MobileApp",
            Unit = unit,
            Kind = kind,
            Temporality = kind == TelemetryMetricKind.Gauge ? TelemetryTemporality.Unspecified : TelemetryTemporality.Delta,
            IsMonotonic = name == "dotnet.exceptions",
            AttributesJson = attributesJson
        };

    private static TelemetryMetric CreateMetric(TelemetryMetricSeriesEntity series, long time, double value)
    {
        var metric = new TelemetryMetric { Series = series };
        metric.Points.Add(new TelemetryMetricPointEntity { SeriesId = series.Id, TimeUnixNano = time, StartTimeUnixNano = time - (30 * Second), Value = value });
        return metric;
    }
}
