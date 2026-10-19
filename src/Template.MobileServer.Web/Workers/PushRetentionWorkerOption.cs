namespace Template.MobileServer.Web.Workers;

public sealed class PushRetentionWorkerOption
{
    public bool Enable { get; set; } = true;

    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 60;

    [Range(1, 3650)]
    public int Days { get; set; } = 7;
}
