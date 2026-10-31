namespace Template.MobileServer.Web.Infrastructure.Data;

using Microsoft.Data.Sqlite;

using Template.MobileServer.Infrastructure.Data;

public sealed class SqliteShardDbProvider : IShardDbProvider
{
    private const string Extension = ".db";

    private static readonly SearchValues<char> KeyChars = SearchValues.Create("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_");

    private readonly string pragma;

    private SqliteShardDbProviderOption Option { get; }

    public SqliteShardDbProvider(SqliteShardDbProviderOption option)
    {
        Option = option;
        pragma = String.Create(CultureInfo.InvariantCulture, $"PRAGMA synchronous = NORMAL; PRAGMA cache_size = -{option.CacheSize}");
    }

    public IReadOnlyList<string> EnumerateKeys() =>
        Directory.Exists(Option.Root)
            ? Directory.EnumerateFiles(Option.Root, "*" + Extension)
                .Select(static x => Path.GetFileNameWithoutExtension(x))
                .Where(static x => IsValidKey(x))
                .ToList()
            : [];

    public ValueTask<DbConnection> OpenAsync(string key, CancellationToken cancellationToken = default) =>
        OpenAsync(key, SqliteOpenMode.ReadWriteCreate, cancellationToken);

    public async ValueTask<DbConnection?> OpenExistingAsync(string key, CancellationToken cancellationToken = default) =>
        File.Exists(GetPath(key)) ? await OpenAsync(key, SqliteOpenMode.ReadWrite, cancellationToken) : null;

    public bool Delete(string key)
    {
        var path = GetPath(key);

        foreach (var mode in (ReadOnlySpan<SqliteOpenMode>)[SqliteOpenMode.ReadWriteCreate, SqliteOpenMode.ReadWrite])
        {
            using var con = new SqliteConnection(MakeConnectionString(path, mode));
            SqliteConnection.ClearPool(con);
        }

        var exists = File.Exists(path);
        File.Delete(path);
        File.Delete(path + "-wal");
        File.Delete(path + "-shm");
        return exists;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private async ValueTask<DbConnection> OpenAsync(string key, SqliteOpenMode mode, CancellationToken cancellationToken)
    {
        var con = new SqliteConnection(MakeConnectionString(GetPath(key), mode));
        try
        {
            await con.OpenAsync(cancellationToken);

            await using var command = con.CreateCommand();
#pragma warning disable CA2100
            command.CommandText = pragma;
#pragma warning restore CA2100
            await command.ExecuteNonQueryAsync(cancellationToken);

            return con;
        }
        catch
        {
            await con.DisposeAsync();
            throw;
        }
    }

    private string GetPath(string key)
    {
        if (!IsValidKey(key))
        {
            throw new ArgumentException("Invalid key.", nameof(key));
        }

        return Path.Combine(Option.Root, key + Extension);
    }

    private static bool IsValidKey(string key) =>
        (key.Length > 0) && !key.AsSpan().ContainsAnyExcept(KeyChars);

    private static string MakeConnectionString(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = true
        }.ToString();
}
