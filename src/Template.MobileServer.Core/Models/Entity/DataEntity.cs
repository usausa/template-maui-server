namespace Template.MobileServer.Models.Entity;

[Name("Data")]
public sealed class DataEntity
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }
}
