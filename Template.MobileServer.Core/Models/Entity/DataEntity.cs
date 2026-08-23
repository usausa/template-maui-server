namespace Template.MobileServer.Models.Entity;

public sealed class DataEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }
}
