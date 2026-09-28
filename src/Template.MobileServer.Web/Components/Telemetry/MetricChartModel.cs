namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

// 点の値の出し方
public enum MetricValueMode
{
    // 平均 (ゲージ、Cumulative の合計、単調でない合計)
    Average,

    // 1 分あたり (単調な Delta の合計。点の区間の長さで割る)
    Rate,

    // 平均 = Sum / Count と回数 (ヒストグラム、指数ヒストグラム、サマリー)
    Distribution
}

// 時間の束の集計 (受信の通知の点を足せる形)
public sealed class MetricBucket
{
    private const long NanosecondsPerMinute = 60_000_000_000;

    public int ValueCount { get; set; }

    public double TotalValue { get; set; }

    public long Duration { get; set; }

    public long Count { get; set; }

    public double Sum { get; set; }

    public double? Max { get; set; }

    public void Add(TelemetryMetricPointEntity point)
    {
        if (point.Value is { } value)
        {
            ValueCount++;
            TotalValue += value;
        }

        Duration += point.TimeUnixNano - point.StartTimeUnixNano;
        Count += point.Count ?? 0;
        Sum += point.Sum ?? 0;
        if (point.Max is { } max)
        {
            Max = Max is { } current ? Math.Max(current, max) : max;
        }
    }

    // 単位を変える前の値 (無ければ null)
    public double? GetValue(MetricValueMode mode) => mode switch
    {
        MetricValueMode.Rate => Duration > 0 ? TotalValue * NanosecondsPerMinute / Duration : null,
        MetricValueMode.Distribution => Count > 0 ? Sum / Count : null,
        _ => ValueCount > 0 ? TotalValue / ValueCount : null
    };
}

// 属性の組み合わせ (系列) ごとの線。束は始まりの時刻 (UTC の Unix ナノ秒) の順
public sealed class MetricLine
{
    public long SeriesId { get; }

    // 属性の値 (既知の計器で 1 本だけの線は空)
    public string Label { get; }

    public string Color { get; }

    // 読み込んだ集計の最後の点の時刻 (これまでの通知の点は読み込み済みとして足さない)
    public long LoadedUntil { get; set; }

    public SortedDictionary<long, MetricBucket> Buckets { get; } = [];

    public MetricLine(long seriesId, string label, string color)
    {
        SeriesId = seriesId;
        Label = label;
        Color = color;
    }
}

// 計器ごとのグラフ。値は単位を変えた後 (Scale 倍)
public sealed class MetricChart
{
    private readonly List<MetricLine> lines = [];

    public string Name { get; }

    public string Title { get; }

    public string Unit { get; }

    public MetricValueMode Mode { get; }

    public double Scale { get; }

    // 並びの順 (既知の計器の順、アプリケーション固有値は既知の計器の後に番号の順、それ以外は最後)
    public long Order { get; }

    // 属性の値の順
    public IReadOnlyList<MetricLine> Lines => lines;

    public MetricChart(string name, string title, string unit, MetricValueMode mode, double scale, long order)
    {
        Name = name;
        Title = title;
        Unit = unit;
        Mode = mode;
        Scale = scale;
        Order = order;
    }

    public double? GetValue(MetricBucket bucket) => bucket.GetValue(Mode) * Scale;

    public double? GetMax(MetricBucket bucket) => bucket.Max * Scale;

    public void AddLine(MetricLine line)
    {
        lines.Add(line);
        lines.Sort(static (x, y) => String.CompareOrdinal(x.Label, y.Label));
    }
}

// メトリクスのタブのグラフ。読み込んだ集計に受信の通知の点を足し、範囲から外れた束を消す。既知の計器は名前と単位を整えて先に並べる
public sealed class MetricChartModel
{
    private const double Mebibyte = 1024 * 1024;

    // アプリケーション固有値の計器の名前 (後ろに番号)
    private const string CustomValuePrefix = "application.custom.value";

    private static readonly string[] Colors = ["#1E88E5", "#E53935", "#43A047", "#FB8C00", "#8E24AA", "#00ACC1", "#6D4C41", "#546E7A"];

    private static readonly KnownInstrument[] KnownInstruments =
    [
        new("process.cpu.utilization", "CPU", "%", 100, []),
        new("process.memory.usage", "メモリ", "MB", 1 / Mebibyte, []),
        new("application.gc.last_collection.heap.size", "ヒープ", "MB", 1 / Mebibyte, []),
        new("process.thread.count", "スレッド", "個", 1, []),
        new("hw.battery.charge", "電池", "%", 100, []),
        new("application.wifi.signal_strength", "無線 LAN", "dBm", 1, []),
        new("dotnet.gc.collections", "GC", "回 / 分", 1, ["gc.heap.generation"]),
        new("dotnet.gc.heap.total_allocated", "割り当て", "MB / 分", 1 / Mebibyte, []),
        new("dotnet.exceptions", "例外", "件 / 分", 1, ["error.type"]),
        new("http.client.request.duration", "HTTP", "ms", 1000, ["http.request.method", "http.response.status_code", "server.address"])
    ];

    private readonly Dictionary<long, MetricLine> lines = [];

    private readonly List<MetricChart> charts = [];

    // 束の長さ (ナノ秒。束ねないときは 1)
    public long Interval { get; }

    public IReadOnlyList<MetricChart> Charts => charts;

    public MetricChartModel(long interval)
    {
        Interval = interval;
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public void Load(TelemetryMetricHistoryView history)
    {
        var series = history.Series.ToDictionary(static x => x.Id);
        foreach (var bucket in history.Buckets)
        {
            if (!series.TryGetValue(bucket.SeriesId, out var entity))
            {
                continue;
            }

            var line = GetOrAddLine(entity);
            line.Buckets[bucket.BucketUnixNano] = new MetricBucket
            {
                ValueCount = bucket.ValueCount,
                TotalValue = bucket.TotalValue ?? 0,
                Duration = bucket.Duration,
                Count = bucket.Count ?? 0,
                Sum = bucket.Sum ?? 0,
                Max = bucket.Max
            };
            line.LoadedUntil = Math.Max(line.LoadedUntil, bucket.LastUnixNano);
        }
    }

    // 受信の通知の点を足す (範囲より前の点と、読み込み済みの時刻までの点は足さない)。足したら true
    public bool Add(IEnumerable<TelemetryMetric> metrics, long since)
    {
        var added = false;
        foreach (var metric in metrics)
        {
            var line = lines.GetValueOrDefault(metric.Series.Id);
            foreach (var point in metric.Points)
            {
                if ((point.TimeUnixNano < since) || (point.TimeUnixNano <= (line?.LoadedUntil ?? 0)))
                {
                    continue;
                }

                line ??= GetOrAddLine(metric.Series);
                var key = ToBucket(point.TimeUnixNano);
                if (!line.Buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new MetricBucket();
                    line.Buckets.Add(key, bucket);
                }

                bucket.Add(point);
                added = true;
            }
        }

        return added;
    }

    // 範囲より前の束を消す
    public void Trim(long since)
    {
        var first = ToBucket(since);
        foreach (var line in lines.Values)
        {
            while ((line.Buckets.Count > 0) && (line.Buckets.Keys.First() < first))
            {
                line.Buckets.Remove(line.Buckets.Keys.First());
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private long ToBucket(long time) => time / Interval * Interval;

    private MetricLine GetOrAddLine(TelemetryMetricSeriesEntity series)
    {
        if (lines.TryGetValue(series.Id, out var line))
        {
            return line;
        }

        var known = Array.Find(KnownInstruments, x => x.Name == series.Name);
        var chart = charts.Find(x => x.Name == series.Name) ?? AddChart(series, known);
        line = new MetricLine(series.Id, MakeLabel(series.AttributesJson, known?.LabelKeys), Colors[chart.Lines.Count % Colors.Length]);
        chart.AddLine(line);
        lines.Add(series.Id, line);
        return line;
    }

    private MetricChart AddChart(TelemetryMetricSeriesEntity series, KnownInstrument? known)
    {
        var mode = ToMode(series);
        MetricChart chart;
        if (known is not null)
        {
            chart = new MetricChart(series.Name, known.Title, known.Unit, mode, known.Scale, Array.IndexOf(KnownInstruments, known));
        }
        else
        {
            // アプリケーション固有値は「値 番号」、それ以外は計器の名前
            var unit = mode == MetricValueMode.Rate ? $"{series.Unit} / 分" : series.Unit;
            chart = TryGetCustomValueNumber(series.Name, out var number)
                ? new MetricChart(series.Name, $"値 {number}", unit, mode, 1, (long)KnownInstruments.Length + number)
                : new MetricChart(series.Name, series.Name, unit, mode, 1, Int64.MaxValue);
        }

        charts.Add(chart);
        charts.Sort(static (x, y) => x.Order != y.Order ? x.Order.CompareTo(y.Order) : String.CompareOrdinal(x.Name, y.Name));
        return chart;
    }

    // アプリケーション固有値 (application.custom.value1〜) の番号
    private static bool TryGetCustomValueNumber(string name, out int number)
    {
        number = 0;
        return name.StartsWith(CustomValuePrefix, StringComparison.Ordinal) &&
               Int32.TryParse(name.AsSpan(CustomValuePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out number) &&
               (number > 0);
    }

    private static MetricValueMode ToMode(TelemetryMetricSeriesEntity series) => series.Kind switch
    {
        TelemetryMetricKind.Histogram or TelemetryMetricKind.ExponentialHistogram or TelemetryMetricKind.Summary => MetricValueMode.Distribution,
        TelemetryMetricKind.Sum when series.IsMonotonic && (series.Temporality == TelemetryTemporality.Delta) => MetricValueMode.Rate,
        _ => MetricValueMode.Average
    };

    // 属性の値を空白でつなぐ (keys が null なら全部)
    private static string MakeLabel(string attributesJson, IReadOnlyList<string>? keys)
    {
        using var document = JsonDocument.Parse(attributesJson);
        var root = document.RootElement;
        var values = keys is null
            ? root.EnumerateObject().Select(static x => FormatAttribute(x.Value)).ToList()
            : keys.Where(x => root.TryGetProperty(x, out _)).Select(x => FormatAttribute(root.GetProperty(x))).ToList();
        return String.Join(' ', values);
    }

    private static string FormatAttribute(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();

    // 既知の計器の名前・単位・倍率と、線の名前にする属性
    private sealed record KnownInstrument(string Name, string Title, string Unit, double Scale, string[] LabelKeys);
}
