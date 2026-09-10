namespace Template.MobileServer.Models.Entity;

public sealed class AccountEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

#pragma warning disable CA1819
    public byte[] Password { get; set; } = default!;
#pragma warning restore CA1819

    public string Role { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
