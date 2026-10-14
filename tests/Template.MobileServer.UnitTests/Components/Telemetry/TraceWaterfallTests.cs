namespace Template.MobileServer.Components.Telemetry;

using Bunit;

using Template.MobileServer.Domain;
using Template.MobileServer.Models.Entity;
using Template.MobileServer.Web.Components.Telemetry;

public sealed class TraceWaterfallTests : MudBlazorTestBase
{
    // 字下げは深さ × 16 px、バーはトレースの長さに対する位置と長さ (最低 2 px の重ね)、イベントは時刻の位置 (例外は太い印)
    [Fact]
    public void RowsShowIndentBarAndEvents()
    {
        // Arrange
        var root = CreateSpan("a", string.Empty, "Root", 0, 1000);
        root.EventsJson = "[{\"time\":250,\"name\":\"exception\",\"attributes\":{}}]";
        var child = CreateSpan("b", "a", "Child", 100, 300);
        child.StatusCode = TelemetryStatusCode.Error;

        // Act
        var cut = Render<TraceWaterfall>(parameters => parameters
            .Add(static x => x.Model, new WaterfallModel([root, child])));

        // Assert
        Assert.Equal("0%", cut.Find(".waterfall-tick").GetAttribute("x1"));
        var rows = cut.FindAll(".waterfall-row:not(.waterfall-head)");
        Assert.Equal("0", rows[0].QuerySelector(".waterfall-indent")!.GetAttribute("width"));
        Assert.Equal("16", rows[1].QuerySelector(".waterfall-indent")!.GetAttribute("width"));
        var bars = rows[1].QuerySelectorAll(".waterfall-bar");
        Assert.Equal("10%", bars[0].GetAttribute("x"));
        Assert.Equal("20%", bars[0].GetAttribute("width"));
        Assert.Contains("waterfall-bar-error", bars[0].GetAttribute("class"), StringComparison.Ordinal);
        Assert.Equal("2", bars[1].GetAttribute("width"));
        var marker = rows[0].QuerySelector(".waterfall-event")!;
        Assert.Equal("25%", marker.GetAttribute("x"));
        Assert.Equal("3", marker.GetAttribute("width"));
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
