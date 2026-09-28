namespace Template.MobileServer.Models.Entity;

// 受信した Resource から取った端末の情報 (端末のファイルに 1 行)。時刻は UTC の Unix ナノ秒
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
