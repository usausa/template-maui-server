namespace Template.MobileServer.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    [Required]
    public string Root { get; set; } = default!;
}
