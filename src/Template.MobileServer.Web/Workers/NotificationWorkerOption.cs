namespace Template.MobileServer.Web.Workers;

public sealed class NotificationWorkerOption
{
    public bool Enable { get; set; }

    [Range(5, 86400)]
    public int IntervalSeconds { get; set; } = 60;
}
