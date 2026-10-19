namespace Template.MobileServer.Web.Telemetry;

public sealed class TelemetryStorageOption
{
    [Required]
    public string Root { get; set; } = default!;

    [Range(64, 65_536)]
    public int CacheSize { get; set; } = 256;
}
