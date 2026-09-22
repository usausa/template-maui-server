namespace Template.MobileServer.Web.Workers;

public sealed class ServerStatusWorkerOption
{
    [Range(100, 3_600_000)]
    public int Interval { get; set; }
}
