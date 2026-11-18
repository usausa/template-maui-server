namespace Template.MobileServer.Web.Settings;

public sealed class SqlLogSetting
{
    public bool Enable { get; set; }

    public bool OutputParameter { get; set; } = true;

    [Range(0, 60_000)]
    public int ElapsedThresholdMilliseconds { get; set; }
}
