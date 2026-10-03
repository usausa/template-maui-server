namespace Template.MobileServer.Models.Entity;

// 端末への通知 (data.db)。宛先の端末ごとに 1 行。DeliveredAt は端末が受け取りを応答した日時 (未達は null)
[Name("PushMessage")]
public sealed class PushMessageEntity
{
    [Key]
    public long Id { get; set; }

    public string DeviceId { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string Body { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
