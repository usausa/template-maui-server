namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

// 計器のグラフ (SVG の折れ線。系列ごとに線)。幅に合わせて伸縮し、欠けた区間は線を切る (前後が欠けた点は丸で出す)。点が少なければ点ごとにホバーで時刻と値を出す
public sealed partial class TimeSeriesChart
{
    private const double Width = 600;
    private const double Height = 200;
    private const double Left = 56;
    private const double Right = 8;
    private const double Top = 8;
    private const double Bottom = 22;

    private const int PointLimit = 150;
    private const int ValueTickCount = 4;
    private const int TimeTickLimit = 6;

    // 欠けとみなす間隔 (点の間隔の中央値の倍数)
    private const double GapFactor = 3;

    private const long NanosecondsPerMinute = 60_000_000_000;
    private const long NanosecondsPerDay = 1440 * NanosecondsPerMinute;
    private const long NanosecondsPerTick = 100;

    // 時間軸の目盛りの間隔 (分)
    private static readonly long[] TimeSteps = [1, 2, 5, 10, 15, 30, 60, 120, 180, 360, 720, 1440, 2880, 10080];

    private IReadOnlyList<ChartTick> valueTicks = [];

    private IReadOnlyList<ChartTick> timeTicks = [];

    private IReadOnlyList<ChartPath> paths = [];

    private IReadOnlyList<ChartPoint> points = [];

    private IReadOnlyList<LegendItem> legend = [];

    private bool hasData;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    [EditorRequired]
    public MetricChart Chart { get; set; } = default!;

    // 時間軸の範囲 (UTC の Unix ナノ秒)
    [Parameter]
    public long From { get; set; }

    [Parameter]
    public long To { get; set; }

    [Parameter]
    public bool ShowDate { get; set; }

    private static string ViewBox => FormattableString.Invariant($"0 0 {Width} {Height}");

    private static string PlotLeft => Format(Left);

    private static string PlotRight => Format(Width - Right);

    private static string PlotTop => Format(Top);

    private static string PlotBottom => Format(Height - Bottom);

    private static string ValueLabelX => Format(Left - 6);

    private static string TimeLabelY => Format(Height - 6);

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        var lines = Chart.Lines.Select(line => new LineValues(line, CollectValues(line))).ToArray();
        legend = lines.Select(MakeLegend).ToArray();

        var values = lines.SelectMany(static x => x.Values).ToArray();
        hasData = values.Length > 0;
        if (!hasData)
        {
            valueTicks = [];
            timeTicks = [];
            paths = [];
            points = [];
            return;
        }

        var (min, max, step) = MakeValueRange(values.Min(static x => x.Value), values.Max(static x => x.Value));
        valueTicks = MakeValueTicks(min, max, step);
        timeTicks = MakeTimeTicks();
        paths = lines.Where(static x => x.Values.Length > 0).Select(x => new ChartPath(x.Line.Color, MakePath(x.Values, min, max))).ToArray();
        points = lines.SelectMany(x => MakePoints(x, values.Length <= PointLimit, min, max)).ToArray();
    }

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    // 範囲の中の値 (範囲の前から始まる束は範囲の始まりに置く)
    private ChartValue[] CollectValues(MetricLine line) =>
        line.Buckets
            .Where(x => x.Key <= To)
            .Select(x => (Time: Math.Max(x.Key, From), Value: Chart.GetValue(x.Value), Bucket: x.Value))
            .Where(static x => x.Value.HasValue)
            .Select(static x => new ChartValue(x.Time, x.Value!.Value, x.Bucket))
            .ToArray();

    // 0 以上の値は 0 から。区切りのよい間隔に広げる
    private static (double Min, double Max, double Step) MakeValueRange(double min, double max)
    {
        if (min >= 0)
        {
            min = 0;
        }

        if (max <= min)
        {
            max = min + 1;
        }

        var step = NiceStep((max - min) / ValueTickCount);
        return (Math.Floor(min / step) * step, Math.Ceiling(max / step) * step, step);
    }

    // 1・2・5 × 10 のべき乗
    private static double NiceStep(double raw)
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var nice = (raw / magnitude) switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            _ => 10
        };
        return nice * magnitude;
    }

    private static List<ChartTick> MakeValueTicks(double min, double max, double step)
    {
        var ticks = new List<ChartTick>();
        var count = (int)Math.Round((max - min) / step);
        for (var i = 0; i <= count; i++)
        {
            var value = min + (i * step);
            ticks.Add(new ChartTick(Format(ToY(value, min, max)), TelemetryFormat.FormatNumber(value)));
        }

        return ticks;
    }

    //--------------------------------------------------------------------------------
    // Time
    //--------------------------------------------------------------------------------

    // 目盛りは現地時刻の区切り
    private List<ChartTick> MakeTimeTicks()
    {
        var span = To - From;
        var step = TimeSteps.Select(static x => x * NanosecondsPerMinute).FirstOrDefault(x => span / x <= TimeTickLimit, TimeSteps[^1] * NanosecondsPerMinute);
        var offset = TimeZoneInfo.Local.GetUtcOffset(TelemetryFormat.ToDateTimeOffset(To)).Ticks * NanosecondsPerTick;
        var first = (((From + offset + step - 1) / step) * step) - offset;

        var ticks = new List<ChartTick>();
        for (var time = first; time <= To; time += step)
        {
            ticks.Add(new ChartTick(Format(ToX(time)), FormatTick(time, step)));
        }

        return ticks;
    }

    private string FormatTick(long time, long step)
    {
        var format = !ShowDate ? "HH:mm" : step >= NanosecondsPerDay ? "MM/dd" : "MM/dd HH:mm";
        return TelemetryFormat.ToDateTimeOffset(time).LocalDateTime.ToString(format, CultureInfo.InvariantCulture);
    }

    //--------------------------------------------------------------------------------
    // Shape
    //--------------------------------------------------------------------------------

    private string MakePath(ChartValue[] values, double min, double max)
    {
        var gap = GapFactor * MedianSpacing(values);
        var builder = new StringBuilder();
        for (var i = 0; i < values.Length; i++)
        {
            var command = IsGapBefore(values, i, gap) ? 'M' : 'L';
            builder.Append(CultureInfo.InvariantCulture, $"{command}{ToX(values[i].Time):0.#},{ToY(values[i].Value, min, max):0.#} ");
        }

        return builder.ToString().TrimEnd();
    }

    // 前後が欠けた点は線にならないので丸で出す。all なら全部の点をホバー用に置く
    private IEnumerable<ChartPoint> MakePoints(LineValues line, bool all, double min, double max)
    {
        var gap = GapFactor * MedianSpacing(line.Values);
        for (var i = 0; i < line.Values.Length; i++)
        {
            var single = IsGapBefore(line.Values, i, gap) && IsGapBefore(line.Values, i + 1, gap);
            if (all || single)
            {
                yield return MakePoint(line.Line, line.Values[i], single, min, max);
            }
        }
    }

    // 最初の点の前と最後の点の後も欠けとみなす
    private static bool IsGapBefore(ChartValue[] values, int index, double gap) =>
        (index == 0) || (index >= values.Length) || (values[index].Time - values[index - 1].Time > gap);

    private static double MedianSpacing(ChartValue[] values)
    {
        if (values.Length < 2)
        {
            return double.MaxValue;
        }

        var spacings = new long[values.Length - 1];
        for (var i = 1; i < values.Length; i++)
        {
            spacings[i - 1] = values[i].Time - values[i - 1].Time;
        }

        Array.Sort(spacings);
        return spacings[spacings.Length / 2];
    }

    private ChartPoint MakePoint(MetricLine line, ChartValue value, bool single, double min, double max) =>
        new(Format(ToX(value.Time)), Format(ToY(value.Value, min, max)), single ? line.Color : "transparent", MakeTitle(line, value));

    // 時刻と値 (ヒストグラムは最大と回数も)。線が複数なら属性の値を先に付ける
    private string MakeTitle(MetricLine line, ChartValue value)
    {
        var text = $"{TelemetryFormat.FormatTime(value.Time)} {FormatValue(value.Value)}";
        if (Chart.Mode == MetricValueMode.Distribution)
        {
            text += Chart.GetMax(value.Bucket) is { } max ? $"・最大 {FormatValue(max)}・{value.Bucket.Count} 回" : $"・{value.Bucket.Count} 回";
        }

        return line.Label.Length > 0 ? $"{line.Label}: {text}" : text;
    }

    // 最新値 (ヒストグラムは範囲の回数も)
    private LegendItem MakeLegend(LineValues values)
    {
        var label = values.Line.Label.Length > 0 ? values.Line.Label : Chart.Title;
        var latest = values.Values.Length > 0 ? FormatValue(values.Values[^1].Value) : "-";
        if (Chart.Mode == MetricValueMode.Distribution)
        {
            latest += $"・{values.Line.Buckets.Values.Sum(static x => x.Count)} 回";
        }

        return new LegendItem($"background-color: {values.Line.Color}", label, latest);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private double ToX(long time) => Left + ((double)(time - From) / Math.Max(To - From, 1) * (Width - Left - Right));

    private static double ToY(double value, double min, double max) =>
        Height - Bottom - ((value - min) / (max - min) * (Height - Top - Bottom));

    // % は数字に続ける
    private string FormatValue(double value) =>
        Chart.Unit == "%" ? $"{TelemetryFormat.FormatNumber(value)}%" : $"{TelemetryFormat.FormatNumber(value)} {Chart.Unit}".TrimEnd();

    private static string Format(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private sealed record ChartValue(long Time, double Value, MetricBucket Bucket);

    private sealed record LineValues(MetricLine Line, ChartValue[] Values);

    private sealed record ChartTick(string Position, string Label);

    private sealed record ChartPath(string Color, string Data);

    private sealed record ChartPoint(string X, string Y, string Fill, string Title);

    private sealed record LegendItem(string SwatchStyle, string Label, string Value);
}
