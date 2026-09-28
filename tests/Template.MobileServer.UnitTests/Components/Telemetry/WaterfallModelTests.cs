namespace Template.MobileServer.Components.Telemetry;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Models.Enums;
using Template.MobileServer.Web.Components.Telemetry;

public sealed class WaterfallModelTests
{
    private static readonly TelemetrySpanEntity[] Spans =
    [
        CreateSpan("b", "a", "Child2", 300, 400),
        CreateSpan("a", string.Empty, "Root", 0, 1000),
        CreateSpan("c", "a", "Child1", 100, 200),
        CreateSpan("d", "c", "Grandchild", 120, 180),
        CreateSpan("e", "x", "Orphan", 500, 600)
    ];

    // 木の順: 親の次に子 (兄弟は開始の順)。親の届いていないスパンは最上位。位置と幅はトレースの長さに対する割合
    [Fact]
    public void RowsAreInTreeOrder()
    {
        // Act
        var model = new WaterfallModel(Spans);

        // Assert
        Assert.Equal(["Root", "Child1", "Grandchild", "Child2", "Orphan"], model.Rows.Select(static x => x.Span.Name));
        Assert.Equal([0, 1, 2, 1, 0], model.Rows.Select(static x => x.Depth));
        Assert.Equal([true, true, false, false, false], model.Rows.Select(static x => x.HasChildren));
        Assert.Equal(0.1, model.Rows[1].Offset, 6);
        Assert.Equal(0.1, model.Rows[1].Width, 6);
    }

    // 開閉: 閉じた行の子孫を隠し、読み直しでは閉じた行を引き継ぐ
    [Fact]
    public void ToggleHidesDescendants()
    {
        // Arrange
        var model = new WaterfallModel(Spans);

        // Act / Assert: 子を閉じる
        model.Toggle("c");
        Assert.Equal(["Root", "Child1", "Child2", "Orphan"], model.VisibleRows.Select(static x => x.Span.Name));

        // Act / Assert: 親も閉じる
        model.Toggle("a");
        Assert.Equal(["Root", "Orphan"], model.VisibleRows.Select(static x => x.Span.Name));

        // Act / Assert: 読み直し
        var reloaded = new WaterfallModel(Spans, model.CollapsedSpans);
        reloaded.Toggle("a");
        Assert.Equal(["Root", "Child1", "Child2", "Orphan"], reloaded.VisibleRows.Select(static x => x.Span.Name));
    }

    // イベント: 時刻の位置と例外の印
    [Fact]
    public void EventsHavePositionAndExceptionMark()
    {
        // Arrange
        var span = CreateSpan("a", string.Empty, "Root", 0, 1000);
        span.EventsJson = "[{\"time\":250,\"name\":\"exception\",\"attributes\":{}},{\"time\":500,\"name\":\"retry\",\"attributes\":{}}]";

        // Act
        var model = new WaterfallModel([span]);

        // Assert
        var events = model.Rows[0].Events;
        Assert.Equal(0.25, events[0].Position, 6);
        Assert.True(events[0].IsException);
        Assert.False(events[1].IsException);
    }

    private static TelemetrySpanEntity CreateSpan(string spanId, string parentSpanId, string name, long start, long end) =>
        new()
        {
            TraceId = "trace-1",
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
}
