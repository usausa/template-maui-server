namespace Template.MobileServer.Web.Telemetry;

using System.Collections.Concurrent;

using Microsoft.Data.Sqlite;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Telemetry;

// 端末ごとの SQLite ファイル (<Root>/<端末 ID>.db)。プロセスで最初に開くときにスキーマ (WAL を含む) を確かめ、開くたびに PRAGMA を設定する
public sealed class SqliteTelemetryDbProvider : ITelemetryDbProvider
{
    private const string Extension = ".db";

    private const string SchemaPath = "Assets/Data/TelemetrySchema.sql";

    private readonly ConcurrentDictionary<string, bool> prepared = new(StringComparer.Ordinal);

    private readonly Lazy<string> schema = new(static () => File.ReadAllText(SchemaPath));

    private readonly TelemetryStorageOption option;

    private readonly GenericAccessor genericAccessor;

    public SqliteTelemetryDbProvider(
        TelemetryStorageOption option,
        GenericAccessor genericAccessor)
    {
        this.option = option;
        this.genericAccessor = genericAccessor;
    }

    public IReadOnlyList<string> EnumerateDevices() =>
        Directory.Exists(option.Root)
            ? Directory.EnumerateFiles(option.Root, "*" + Extension)
                .Select(static x => Path.GetFileNameWithoutExtension(x))
                .Where(static x => DeviceIdFormat.IsValid(x))
                .ToList()
            : [];

    public ValueTask<DbConnection> OpenAsync(string deviceId, CancellationToken cancellationToken) =>
        OpenAsync(deviceId, SqliteOpenMode.ReadWriteCreate, cancellationToken);

    public async ValueTask<DbConnection?> OpenExistingAsync(string deviceId, CancellationToken cancellationToken) =>
        File.Exists(GetPath(deviceId)) ? await OpenAsync(deviceId, SqliteOpenMode.ReadWrite, cancellationToken) : null;

    public bool Delete(string deviceId)
    {
        var path = GetPath(deviceId);

        // Close the idle connections of the pool, otherwise the files are in use
        foreach (var mode in (ReadOnlySpan<SqliteOpenMode>)[SqliteOpenMode.ReadWriteCreate, SqliteOpenMode.ReadWrite])
        {
            using var con = new SqliteConnection(MakeConnectionString(path, mode));
            SqliteConnection.ClearPool(con);
        }

        prepared.TryRemove(deviceId, out _);

        var exists = File.Exists(path);
        File.Delete(path);
        File.Delete(path + "-wal");
        File.Delete(path + "-shm");
        return exists;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private async ValueTask<DbConnection> OpenAsync(string deviceId, SqliteOpenMode mode, CancellationToken cancellationToken)
    {
        var con = new SqliteConnection(MakeConnectionString(GetPath(deviceId), mode));
        try
        {
            await con.OpenAsync(cancellationToken);

            // A new file has the version 0, the schema sets the version
            if (!prepared.ContainsKey(deviceId))
            {
                if (await genericAccessor.QueryUserVersionAsync(con, cancellationToken) == 0)
                {
                    await genericAccessor.ExecuteSchemaAsync(con, schema.Value, cancellationToken);
                }

                prepared[deviceId] = true;
            }

            await genericAccessor.ExecuteTelemetryPragmaAsync(con, option.CacheSize, cancellationToken);

            return con;
        }
        catch
        {
            await con.DisposeAsync();
            throw;
        }
    }

    private string GetPath(string deviceId)
    {
        if (!DeviceIdFormat.IsValid(deviceId))
        {
            throw new ArgumentException("Invalid device id.", nameof(deviceId));
        }

        return Path.Combine(option.Root, deviceId + Extension);
    }

    private static string MakeConnectionString(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = true
        }.ToString();
}
