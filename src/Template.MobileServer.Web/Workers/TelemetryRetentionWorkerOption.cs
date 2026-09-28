namespace Template.MobileServer.Web.Workers;

public sealed class TelemetryRetentionWorkerOption
{
    public bool Enable { get; set; } = true;

    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 60;

    [Range(1, 3650)]
    public int LogDays { get; set; } = 7;

    [Range(1, 3650)]
    public int TraceDays { get; set; } = 7;

    [Range(1, 3650)]
    public int MetricDays { get; set; } = 30;

    // 最後の受信からこの日数を過ぎた端末は、テレメトリのファイルを削除する (登録は残る)
    [Range(1, 3650)]
    public int DeviceDays { get; set; } = 30;
}
