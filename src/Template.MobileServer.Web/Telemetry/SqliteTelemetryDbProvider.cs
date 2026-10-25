namespace Template.MobileServer.Web.Telemetry;

using System.Collections.Concurrent;

using Microsoft.Data.Sqlite;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Telemetry;

public sealed class SqliteTelemetryDbProvider : ITelemetryDbProvider
{
    private const string Extension = ".db";

    private const string SchemaPath = "Assets/Data/TelemetrySchema.sql";

    private readonly ConcurrentDictionary<string, bool> prepared = new(StringComparer.Ordinal);

    private readonly Lazy<string> schema = new(static () => File.ReadAllText(SchemaPath));

    private TelemetryStorageOption Option { get; }

    private GenericAccessor GenericAccessor { get; }

    public SqliteTelemetryDbProvider(
        TelemetryStorageOption option,
        GenericAccessor genericAccessor)
    {
        Option = option;
        GenericAccessor = genericAccessor;
    }

    public IReadOnlyList<string> EnumerateDevices() =>
        Directory.Exists(Option.Root)
            ? Directory.EnumerateFiles(Option.Root, "*" + Extension)
                .Select(static x => Path.GetFileNameWithoutExtension(x))
                .Where(static x => TerminalIdFormat.IsValid(x))
                .ToList()
            : [];

    public ValueTask<DbConnection> OpenAsync(string terminalId, CancellationToken cancellationToken = default) =>
        OpenAsync(terminalId, SqliteOpenMode.ReadWriteCreate, cancellationToken);

    public async ValueTask<DbConnection?> OpenExistingAsync(string terminalId, CancellationToken cancellationToken = default) =>
        File.Exists(GetPath(terminalId)) ? await OpenAsync(terminalId, SqliteOpenMode.ReadWrite, cancellationToken) : null;

    public bool Delete(string terminalId)
    {
        var path = GetPath(terminalId);

        foreach (var mode in (ReadOnlySpan<SqliteOpenMode>)[SqliteOpenMode.ReadWriteCreate, SqliteOpenMode.ReadWrite])
        {
            using var con = new SqliteConnection(MakeConnectionString(path, mode));
            SqliteConnection.ClearPool(con);
        }

        prepared.TryRemove(terminalId, out _);

        var exists = File.Exists(path);
        File.Delete(path);
        File.Delete(path + "-wal");
        File.Delete(path + "-shm");
        return exists;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private async ValueTask<DbConnection> OpenAsync(string terminalId, SqliteOpenMode mode, CancellationToken cancellationToken)
    {
        var con = new SqliteConnection(MakeConnectionString(GetPath(terminalId), mode));
        try
        {
            await con.OpenAsync(cancellationToken);

            if (!prepared.ContainsKey(terminalId))
            {
                if (await GenericAccessor.QueryUserVersionAsync(con) == 0)
                {
                    await GenericAccessor.ExecuteSchemaAsync(con, schema.Value);
                }

                prepared[terminalId] = true;
            }

            await GenericAccessor.ExecuteTelemetryPragmaAsync(con, Option.CacheSize);

            return con;
        }
        catch
        {
            await con.DisposeAsync();
            throw;
        }
    }

    private string GetPath(string terminalId)
    {
        if (!TerminalIdFormat.IsValid(terminalId))
        {
            throw new ArgumentException("Invalid terminal id.", nameof(terminalId));
        }

        return Path.Combine(Option.Root, terminalId + Extension);
    }

    private static string MakeConnectionString(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = true
        }.ToString();
}
