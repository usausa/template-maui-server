namespace Template.MobileServer.Models.Entity;

[Name("Device")]
public sealed class DeviceEntity
{
    [Key]
    public string DeviceId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? GroupName { get; set; }

    public string? Note { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime RegisteredAt { get; set; }
}
