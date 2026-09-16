namespace Template.MobileServer.Web.Settings;

public sealed class TelemetrySetting
{
    [Range(1, 3600000)]
    public int LongExecutionThreshold { get; set; }
}
