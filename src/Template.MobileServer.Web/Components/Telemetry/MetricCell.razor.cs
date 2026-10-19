namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Template.MobileServer.Web.Telemetry;

public enum MetricDisplay
{
    Battery,

    Utilization,

    Signal,

    Bytes,

    CustomValue
}

public sealed partial class MetricCell
{
    private bool showIcon;

    private bool showMeter;

    private string icon = string.Empty;

    private Color color;

    private string fillClass = string.Empty;

    private string fillWidth = "0";

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
        showIcon = Display is MetricDisplay.Battery or MetricDisplay.Signal;
        showMeter = Display is MetricDisplay.Utilization or MetricDisplay.CustomValue;
        icon = Display switch
        {
            MetricDisplay.Battery => ViewHelper.BatteryIcon(value.Value),
            MetricDisplay.Signal => ViewHelper.SignalIcon(value.Value),
            _ => string.Empty
        };
        color = ViewHelper.LevelColor(level);
        fillClass = $"meter-fill {ViewHelper.LevelClass(level)}";
        fillWidth = FormattableString.Invariant($"{Math.Clamp(ToRatio(Display, value.Value), 0, 1) * 100:0.#}%");
        text = showIcon ? string.Empty : formatted;
        title = $"{formatted} ({ViewHelper.FormatTime(value.TimeUnixNano)})";
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string Format(MetricDisplay display, double value) => display switch
    {
        MetricDisplay.Battery => ViewHelper.FormatPercent(value),
        MetricDisplay.Utilization => ViewHelper.FormatUtilization(value),
        MetricDisplay.Signal => ViewHelper.FormatSignal(value),
        MetricDisplay.CustomValue => ViewHelper.FormatCustomValue(value),
        _ => ViewHelper.FormatBytes((long)value)
    };

    private static double ToRatio(MetricDisplay display, double value) =>
        display == MetricDisplay.CustomValue ? value / 100 : value;

    private static TelemetryLevel ToLevel(MetricDisplay display, double value) => display switch
    {
        MetricDisplay.Battery => ViewHelper.BatteryLevel(value),
        MetricDisplay.Utilization => ViewHelper.UtilizationLevel(value),
        MetricDisplay.Signal => ViewHelper.SignalLevel(value),
        _ => TelemetryLevel.None
    };
}
