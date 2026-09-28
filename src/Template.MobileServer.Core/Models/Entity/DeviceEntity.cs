namespace Template.MobileServer.Models.Entity;

// 端末の登録 (data.db)。受信で自動登録するか、管理画面・端末から登録する
[Name("Devices")]
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
