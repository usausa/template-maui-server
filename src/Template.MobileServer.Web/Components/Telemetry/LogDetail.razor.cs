namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

using Microsoft.AspNetCore.Components;

// ログの詳細 (本文の全体、重大度・イベント・観測時刻・スコープ・ID、属性、例外のスタックトレース、Resource)
public sealed partial class LogDetail
{
    private const string StackTraceKey = "exception.stacktrace";

    private string? stackTrace;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    [EditorRequired]
    public TelemetryLogEntity Log { get; set; } = default!;

    [Parameter]
    public TelemetryResourceEntity? Resource { get; set; }

    private string SeverityText => $"{TelemetryFormat.FormatSeverity(Log.SeverityNumber)} ({Log.SeverityNumber}・{Log.SeverityText})";

    private string EventText => Log.EventName.Length > 0 ? Log.EventName : "-";

    private string ObservedText => TelemetryFormat.FormatTime(Log.ObservedTimeUnixNano);

    private string TraceText => Log.TraceId.Length > 0 ? Log.TraceId : "-";

    private string SpanText => Log.SpanId.Length > 0 ? Log.SpanId : "-";

    private bool HasStackTrace => stackTrace is not null;

    private bool HasResource => Resource is not null;

    private string? ResourceJson => Resource?.AttributesJson;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    // 例外のスタックトレースは属性の表から外して別に出す
    protected override void OnParametersSet()
    {
        using var document = JsonDocument.Parse(Log.AttributesJson);
        stackTrace = document.RootElement.TryGetProperty(StackTraceKey, out var value) ? AttributeTable.FormatValue(value) : null;
    }
}
