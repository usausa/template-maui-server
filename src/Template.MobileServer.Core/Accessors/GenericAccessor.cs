namespace Template.MobileServer.Accessors;

[DataAccessor]
public sealed partial class GenericAccessor
{
    [DirectSql]
    [Execute]
    public partial ValueTask<int> ExecuteSchemaAsync(DbConnection con, string sql, CancellationToken cancellationToken);

    // スキーマの版 (新しいファイルは 0)
    [ExecuteScalar]
    public partial ValueTask<long> QueryUserVersionAsync(DbConnection con, CancellationToken cancellationToken);

    // 端末ごとのテレメトリの DB の接続の設定 (接続ごとに必要。cacheSize は KiB)
    [Execute]
    public partial ValueTask<int> ExecuteTelemetryPragmaAsync(DbConnection con, int cacheSize, CancellationToken cancellationToken);
}
