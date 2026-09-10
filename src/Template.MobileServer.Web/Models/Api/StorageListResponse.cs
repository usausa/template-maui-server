namespace Template.MobileServer.Web.Models.Api;

// [配置区分] Models/Api: モバイル契約DTO(PascalCaseのJSON契約)
// [MEMO] 一覧エントリーは種別/サイズ/更新日を含む契約拡張版(クライアント側は一覧未使用のため互換影響なし)
public sealed class StorageListResponseEntry
{
    public string Name { get; set; } = default!;

    public bool Directory { get; set; }

    // ディレクトリの場合はnull(JSON出力では省略)
    public long? Size { get; set; }

    public DateTime LastModified { get; set; }
}

public sealed class StorageListResponse
{
    public IReadOnlyList<StorageListResponseEntry> Entries { get; set; } = default!;
}
