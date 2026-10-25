namespace Template.MobileServer.Models.Entity;

[Name("PushMessage")]
public sealed class PushMessageEntity
{
    [Key]
    public long Id { get; set; }

    public string TerminalId { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string Body { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}
