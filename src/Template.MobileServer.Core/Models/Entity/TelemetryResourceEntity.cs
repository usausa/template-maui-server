namespace Template.MobileServer.Models.Entity;

// Resource (プロセスの起動ごとに変わる)。Hash は属性をキー順に並べた JSON の SHA-256
[Name("Resource")]
public sealed class TelemetryResourceEntity
{
    [Key]
    public long Id { get; set; }

    public string Hash { get; set; } = default!;

    public string ServiceInstanceId { get; set; } = default!;

    public string ServiceVersion { get; set; } = default!;

    public string AttributesJson { get; set; } = default!;

    public long FirstSeenAt { get; set; }
}
