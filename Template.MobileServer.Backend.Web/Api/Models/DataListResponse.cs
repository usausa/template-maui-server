namespace Template.MobileServer.Backend.Web.Api.Models;

public sealed class DataListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

#pragma warning disable CA1819
public sealed class DataListResponse
{
    public DataListResponseEntry[] Entries { get; set; } = default!;
}
#pragma warning restore CA1819
