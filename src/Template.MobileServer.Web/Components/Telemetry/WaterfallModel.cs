namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

// スパンのイベントの印 (位置はトレースの開始からの割合)
public sealed record WaterfallEvent(double Position, string Name, bool IsException);

// ウォーターフォールの行。位置と幅はトレースの長さに対する割合 (0〜1)
public sealed class WaterfallRow
{
    public required TelemetrySpanEntity Span { get; init; }

    public required int Depth { get; init; }

    public required bool HasChildren { get; init; }

    public required double Offset { get; init; }

    public required double Width { get; init; }

    public required IReadOnlyList<WaterfallEvent> Events { get; init; }

    public long Duration => Span.EndTimeUnixNano - Span.StartTimeUnixNano;

    public bool IsError => Span.StatusCode == TelemetryStatusCode.Error;
}

// ウォーターフォールの行 (親子の木の順。兄弟は開始の順、親が届いていないスパンは最上位) と開閉
public sealed class WaterfallModel
{
    private readonly HashSet<string> collapsed = [with(StringComparer.Ordinal)];

    // トレースの範囲 (UTC の Unix ナノ秒)
    public long Start { get; }

    public long End { get; }

    public long Duration => Math.Max(End - Start, 1);

    public IReadOnlyList<WaterfallRow> Rows { get; }

    // 閉じた行 (読み直しで引き継ぐ)
    public IReadOnlyCollection<string> CollapsedSpans => collapsed;

    // 閉じた行の子孫を除く
    public IReadOnlyList<WaterfallRow> VisibleRows
    {
        get
        {
            var rows = new List<WaterfallRow>();
            var hiddenDepth = Int32.MaxValue;
            foreach (var row in Rows)
            {
                if (row.Depth > hiddenDepth)
                {
                    continue;
                }

                hiddenDepth = collapsed.Contains(row.Span.SpanId) ? row.Depth : Int32.MaxValue;
                rows.Add(row);
            }

            return rows;
        }
    }

    public WaterfallModel(IReadOnlyList<TelemetrySpanEntity> spans, IEnumerable<string>? collapsedSpans = null)
    {
        Start = spans.Count > 0 ? spans.Min(static x => x.StartTimeUnixNano) : 0;
        End = spans.Count > 0 ? spans.Max(static x => x.EndTimeUnixNano) : 0;
        Rows = BuildRows(spans);
        collapsed.UnionWith(collapsedSpans ?? []);
    }

    public bool IsCollapsed(string spanId) => collapsed.Contains(spanId);

    public void Toggle(string spanId)
    {
        if (!collapsed.Remove(spanId))
        {
            collapsed.Add(spanId);
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private List<WaterfallRow> BuildRows(IReadOnlyList<TelemetrySpanEntity> spans)
    {
        var ids = spans.Select(static x => x.SpanId).ToHashSet(StringComparer.Ordinal);
        var children = spans
            .Where(x => (x.ParentSpanId.Length > 0) && ids.Contains(x.ParentSpanId))
            .GroupBy(static x => x.ParentSpanId, StringComparer.Ordinal)
            .ToDictionary(static x => x.Key, static x => Order(x).ToList(), StringComparer.Ordinal);
        var rows = new List<WaterfallRow>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in Order(spans.Where(x => (x.ParentSpanId.Length == 0) || !ids.Contains(x.ParentSpanId))))
        {
            Add(root, 0);
        }

        // Spans in a parent cycle are not reachable from a root
        foreach (var span in Order(spans.Where(x => !visited.Contains(x.SpanId))))
        {
            Add(span, 0);
        }

        return rows;

        void Add(TelemetrySpanEntity span, int depth)
        {
            if (!visited.Add(span.SpanId))
            {
                return;
            }

            var hasChildren = children.TryGetValue(span.SpanId, out var list);
            rows.Add(new WaterfallRow
            {
                Span = span,
                Depth = depth,
                HasChildren = hasChildren,
                Offset = ToPosition(span.StartTimeUnixNano),
                Width = (double)(span.EndTimeUnixNano - span.StartTimeUnixNano) / Duration,
                Events = ParseEvents(span.EventsJson)
            });

            foreach (var child in list ?? [])
            {
                Add(child, depth + 1);
            }
        }
    }

    private static IOrderedEnumerable<TelemetrySpanEntity> Order(IEnumerable<TelemetrySpanEntity> spans) =>
        spans.OrderBy(static x => x.StartTimeUnixNano).ThenBy(static x => x.SpanId, StringComparer.Ordinal);

    private double ToPosition(long time) => Math.Clamp((double)(time - Start) / Duration, 0, 1);

    // 例外は exception のイベント
    private List<WaterfallEvent> ParseEvents(string? eventsJson)
    {
        if (String.IsNullOrEmpty(eventsJson))
        {
            return [];
        }

        using var document = JsonDocument.Parse(eventsJson);
        return document.RootElement.EnumerateArray()
            .Select(x =>
            {
                var name = x.GetProperty("name").GetString() ?? string.Empty;
                return new WaterfallEvent(ToPosition(x.GetProperty("time").GetInt64()), name, name == "exception");
            })
            .ToList();
    }
}
