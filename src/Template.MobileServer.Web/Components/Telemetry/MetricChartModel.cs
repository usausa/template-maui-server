namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

public enum MetricValueMode
{
    Average,

    Rate,

    Distribution
}

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

    public double? GetValue(MetricValueMode mode) => mode switch
    {
        MetricValueMode.Rate => Duration > 0 ? TotalValue * NanosecondsPerMinute / Duration : null,
        MetricValueMode.Distribution => Count > 0 ? Sum / Count : null,
        _ => ValueCount > 0 ? TotalValue / ValueCount : null
    };
}

public sealed class MetricLine
{
    public long SeriesId { get; }

    public string Label { get; }

    public string Color { get; }

    public long LoadedUntil { get; set; }

    public SortedDictionary<long, MetricBucket> Buckets { get; } = [];

    public MetricLine(long seriesId, string label, string color)
    {
        SeriesId = seriesId;
        Label = label;
        Color = color;
    }
}

public sealed class MetricChart
{
    private readonly List<MetricLine> lines = [];

    public string Name { get; }

    public string Title { get; }

    public string Unit { get; }

    public MetricValueMode Mode { get; }

    public double Scale { get; }

    public long Order { get; }

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

public sealed class MetricChartModel
{
    private const double Mebibyte = 1024 * 1024;

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

    public long Interval { get; }

    public IReadOnlyList<MetricChart> Charts => charts;

    public MetricChartModel(long interval)
    {
        Interval = interval;
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public void Load(TelemetryMetricHistoryResult history)
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
            var unit = mode == MetricValueMode.Rate ? $"{series.Unit} / 分" : series.Unit;
            chart = TryGetCustomValueNumber(series.Name, out var number)
                ? new MetricChart(series.Name, $"値 {number}", unit, mode, 1, (long)KnownInstruments.Length + number)
                : new MetricChart(series.Name, series.Name, unit, mode, 1, Int64.MaxValue);
        }

        charts.Add(chart);
        charts.Sort(static (x, y) => x.Order != y.Order ? x.Order.CompareTo(y.Order) : String.CompareOrdinal(x.Name, y.Name));
        return chart;
    }

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

    private sealed record KnownInstrument(string Name, string Title, string Unit, double Scale, string[] LabelKeys);
}
