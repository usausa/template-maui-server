namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

using Microsoft.AspNetCore.Components;

using MudBlazor;

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

    private string StartText => $"+{ViewHelper.FormatDuration(Span.StartTimeUnixNano - TraceStart)} ({ViewHelper.FormatTime(Span.StartTimeUnixNano)})";

    private string DurationText => ViewHelper.FormatDuration(Span.EndTimeUnixNano - Span.StartTimeUnixNano);

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
                    $"+{ViewHelper.FormatDuration(x.GetProperty("time").GetInt64() - TraceStart)}",
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
