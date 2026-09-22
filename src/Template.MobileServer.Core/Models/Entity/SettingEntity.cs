namespace Template.MobileServer.Models.Entity;

public sealed class SettingEntity
{
    public string Key { get; set; } = default!;

    public string Value { get; set; } = default!;

    public DateTime UpdatedAt { get; set; }
}
