namespace Template.MobileServer.Web.Settings;

public sealed class WorkerSetting
{
    public bool Enable { get; set; }

    [Range(5, 86400)]
    public int IntervalSeconds { get; set; } = 60;
}
