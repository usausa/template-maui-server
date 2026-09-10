namespace Template.MobileServer.Web.Models.Api;

// [配置区分] Models/Api: モバイル契約DTO(PascalCaseのJSON契約)
public sealed class DataListResponseEntry
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;
}

public sealed class DataListResponse
{
    public IReadOnlyList<DataListResponseEntry> Entries { get; set; } = default!;
}
