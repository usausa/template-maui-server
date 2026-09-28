namespace Template.MobileServer.Infrastructure.Telemetry;

// 端末ごとのテレメトリの DB。スキーマと接続の設定は実装が行い、開いた接続を返す
public interface ITelemetryDbProvider
{
    IReadOnlyList<string> EnumerateDevices();

    // ファイルが無ければ作る
    ValueTask<DbConnection> OpenAsync(string deviceId, CancellationToken cancellationToken);

    // ファイルが無ければ null (照会ではファイルを作らない)
    ValueTask<DbConnection?> OpenExistingAsync(string deviceId, CancellationToken cancellationToken);

    // ファイルが無ければ false
    bool Delete(string deviceId);
}
