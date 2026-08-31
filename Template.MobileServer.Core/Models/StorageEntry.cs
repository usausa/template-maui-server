namespace Template.MobileServer.Models;

// ストレージエントリー(ファイル/ディレクトリの一覧情報)
public sealed record StorageEntry(string Name, bool IsDirectory, long Size, DateTime LastModified);
