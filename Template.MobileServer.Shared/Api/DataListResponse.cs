namespace Template.MobileServer.Api;

public class DataListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

#pragma warning disable CA1819
public class DataListResponse
{
    public DataListResponseEntry[] Entries { get; set; } = default!;
}
#pragma warning restore CA1819
