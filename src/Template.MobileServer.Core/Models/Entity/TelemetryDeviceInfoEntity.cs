namespace Template.MobileServer.Models.Entity;

[Name("DeviceInfo")]
public sealed class TelemetryDeviceInfoEntity
{
    [Key]
    public string DeviceId { get; set; } = default!;

    public string InstallationId { get; set; } = default!;

    public string Manufacturer { get; set; } = default!;

    public string Model { get; set; } = default!;

    public string OsName { get; set; } = default!;

    public string OsVersion { get; set; } = default!;

    public string ServiceName { get; set; } = default!;

    public string ServiceVersion { get; set; } = default!;

    public long FirstReceivedAt { get; set; }

    public long LastReceivedAt { get; set; }
}
