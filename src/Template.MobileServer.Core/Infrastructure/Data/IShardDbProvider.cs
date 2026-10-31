namespace Template.MobileServer.Infrastructure.Data;

public interface IShardDbProvider
{
    IReadOnlyList<string> EnumerateKeys();

    ValueTask<DbConnection> OpenAsync(string key, CancellationToken cancellationToken = default);

    ValueTask<DbConnection?> OpenExistingAsync(string key, CancellationToken cancellationToken = default);

    bool Delete(string key);
}
