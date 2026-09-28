namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Template.MobileServer.Web.Telemetry;

// 最新値の表し方
public enum MetricDisplay
{
    // 横棒と % (0〜1)
    Battery,

    // 横棒と % (0〜1。小数 1 桁)
    Utilization,

    // 電波のアイコン (値はホバーで出す)
    Signal,

    // バイト数
    Bytes,

    // 横棒と値 (アプリケーション固有値。0〜100)
    CustomValue
}

// 最新値のセル。色は値の良し悪し、ホバーで値と測った時刻を出す。値が無ければ "-"
public sealed partial class MetricCell
{
    private bool showIcon;

    private bool showMeter;

    private string icon = string.Empty;

    private Color color;

    private string fillClass = string.Empty;

    private string fillStyle = string.Empty;

    private string text = "-";

    private string? title;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public TelemetryValue? Value { get; set; }

    [Parameter]
    public MetricDisplay Display { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        if (Value is not { } value)
        {
            showIcon = false;
            showMeter = false;
            text = "-";
            title = null;
            return;
        }

        var formatted = Format(Display, value.Value);
        var level = ToLevel(Display, value.Value);
        showIcon = Display == MetricDisplay.Signal;
        showMeter = Display is MetricDisplay.Battery or MetricDisplay.Utilization or MetricDisplay.CustomValue;
        icon = showIcon ? TelemetryFormat.SignalIcon(value.Value) : string.Empty;
        color = TelemetryFormat.LevelColor(level);
        fillClass = $"meter-fill {TelemetryFormat.LevelClass(level)}";
        fillStyle = FormattableString.Invariant($"width: {Math.Clamp(ToRatio(Display, value.Value), 0, 1) * 100:0.#}%");
        text = showIcon ? string.Empty : formatted;
        title = $"{formatted} ({TelemetryFormat.FormatTime(value.TimeUnixNano)})";
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string Format(MetricDisplay display, double value) => display switch
    {
        MetricDisplay.Battery => TelemetryFormat.FormatPercent(value),
        MetricDisplay.Utilization => TelemetryFormat.FormatUtilization(value),
        MetricDisplay.Signal => TelemetryFormat.FormatSignal(value),
        MetricDisplay.CustomValue => TelemetryFormat.FormatCustomValue(value),
        _ => ViewHelper.FormatBytes((long)value)
    };

    // 横棒の割合 (固有値は 0〜100)
    private static double ToRatio(MetricDisplay display, double value) =>
        display == MetricDisplay.CustomValue ? value / 100 : value;

    private static TelemetryLevel ToLevel(MetricDisplay display, double value) => display switch
    {
        MetricDisplay.Battery => TelemetryFormat.BatteryLevel(value),
        MetricDisplay.Utilization => TelemetryFormat.UtilizationLevel(value),
        MetricDisplay.Signal => TelemetryFormat.SignalLevel(value),
        _ => TelemetryLevel.None
    };
}
