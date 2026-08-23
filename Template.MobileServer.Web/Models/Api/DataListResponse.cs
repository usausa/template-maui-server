namespace Template.MobileServer.Web.Models.Api;

public sealed class DataListResponseEntry
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;
}

public sealed class DataListResponse
{
    public IReadOnlyList<DataListResponseEntry> Entries { get; set; } = default!;
}
