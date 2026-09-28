namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// スパンの詳細 (種類、状態とメッセージ、開始 (トレースの開始から)・所要時間、ID、属性、イベント (例外のスタックトレース)、リンク、Resource)
public sealed partial class SpanDetail
{
    private const string StackTraceKey = "exception.stacktrace";

    private List<SpanEventItem> events = [];

    private List<SpanLinkItem> links = [];

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    [EditorRequired]
    public TelemetrySpanEntity Span { get; set; } = default!;

    // トレースの開始 (UTC の Unix ナノ秒)
    [Parameter]
    public long TraceStart { get; set; }

    [Parameter]
    public TelemetryResourceEntity? Resource { get; set; }

    private Color StatusColor => Span.StatusCode switch
    {
        TelemetryStatusCode.Error => Color.Error,
        TelemetryStatusCode.Ok => Color.Success,
        _ => Color.Default
    };

    private bool HasStatusMessage => Span.StatusMessage.Length > 0;

    private string StartText => $"+{TelemetryFormat.FormatDuration(Span.StartTimeUnixNano - TraceStart)} ({TelemetryFormat.FormatTime(Span.StartTimeUnixNano)})";

    private string DurationText => TelemetryFormat.FormatDuration(Span.EndTimeUnixNano - Span.StartTimeUnixNano);

    private string ParentText => Span.ParentSpanId.Length > 0 ? Span.ParentSpanId : "-";

    private bool HasEvents => events.Count > 0;

    private bool HasLinks => links.Count > 0;

    private bool HasResource => Resource is not null;

    private string? ResourceJson => Resource?.AttributesJson;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        events = ParseEvents(Span.EventsJson);
        links = ParseLinks(Span.LinksJson);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    // 例外のスタックトレースは属性の表から外して別に出す
    private List<SpanEventItem> ParseEvents(string? json)
    {
        if (String.IsNullOrEmpty(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(x =>
            {
                var attributes = x.GetProperty("attributes");
                return new SpanEventItem(
                    x.GetProperty("name").GetString() ?? string.Empty,
                    $"+{TelemetryFormat.FormatDuration(x.GetProperty("time").GetInt64() - TraceStart)}",
                    attributes.GetRawText(),
                    attributes.TryGetProperty(StackTraceKey, out var stackTrace) ? AttributeTable.FormatValue(stackTrace) : null);
            })
            .ToList();
    }

    private static List<SpanLinkItem> ParseLinks(string? json)
    {
        if (String.IsNullOrEmpty(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(static x => new SpanLinkItem(
                x.GetProperty("traceId").GetString() ?? string.Empty,
                x.GetProperty("spanId").GetString() ?? string.Empty,
                x.GetProperty("attributes").GetRawText()))
            .ToList();
    }

    private sealed record SpanEventItem(string Name, string Offset, string AttributesJson, string? StackTrace)
    {
        public bool HasStackTrace => StackTrace is not null;
    }

    private sealed record SpanLinkItem(string TraceId, string SpanId, string AttributesJson);
}
