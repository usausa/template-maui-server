namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// トレースのウォーターフォール。字下げで深さを示し、子を持つ行は開閉できる。バーはトレースの開始からの位置と長さ (幅は最低 2 px)、エラーは赤、イベントの時刻に印 (例外は赤)
public sealed partial class TraceWaterfall
{
    private const int TickCount = 4;

    private const int IndentWidth = 16;

    private IReadOnlyList<WaterfallTick> ticks = [];

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    [EditorRequired]
    public WaterfallModel Model { get; set; } = default!;

    [Parameter]
    public string? SelectedSpanId { get; set; }

    [Parameter]
    public EventCallback<string> SpanSelected { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    // 目盛りは区切りのよい時間 (1・2・5 × 10 のべき乗)
    protected override void OnParametersSet()
    {
        var step = NiceStep((double)Model.Duration / TickCount);
        var list = new List<WaterfallTick>();
        for (var time = 0d; time <= Model.Duration; time += step)
        {
            list.Add(new WaterfallTick(FormattableString.Invariant($"left: {time / Model.Duration * 100:0.###}%"), ViewHelper.FormatDuration((long)time)));
        }

        ticks = list;
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private Task SelectAsync(WaterfallRow row) => SpanSelected.InvokeAsync(row.Span.SpanId);

    private void Toggle(WaterfallRow row) => Model.Toggle(row.Span.SpanId);

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    private string RowClass(WaterfallRow row) =>
        row.Span.SpanId == SelectedSpanId ? "waterfall-row waterfall-selected" : "waterfall-row";

    private static string IndentStyle(WaterfallRow row) => $"padding-left: {row.Depth * IndentWidth}px";

    private string ToggleIcon(WaterfallRow row) =>
        Model.IsCollapsed(row.Span.SpanId) ? Icons.Material.Filled.ChevronRight : Icons.Material.Filled.ExpandMore;

    private static string BarClass(WaterfallRow row) => row.IsError ? "waterfall-bar waterfall-bar-error" : "waterfall-bar";

    private static string BarStyle(WaterfallRow row) =>
        FormattableString.Invariant($"left: {row.Offset * 100:0.###}%; width: max(2px, {row.Width * 100:0.###}%)");

    private static string EventClass(WaterfallEvent item) => item.IsException ? "waterfall-event waterfall-event-exception" : "waterfall-event";

    private static string EventStyle(WaterfallEvent item) => FormattableString.Invariant($"left: {item.Position * 100:0.###}%");

    private static double NiceStep(double raw)
    {
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(raw, 1))));
        var nice = (raw / magnitude) switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            _ => 10
        };
        return nice * magnitude;
    }

    private sealed record WaterfallTick(string Style, string Label);
}
