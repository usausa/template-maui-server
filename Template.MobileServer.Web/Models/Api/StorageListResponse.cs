namespace Template.MobileServer.Web.Models.Api;

public sealed class StorageListResponse
{
    public IReadOnlyList<string> Entries { get; set; } = default!;
}
