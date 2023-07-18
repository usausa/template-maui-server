namespace Template.MobileServer.Api;

public class DataListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class DataListResponse
{
    public DataListResponseEntry[] Entries { get; set; }
}
