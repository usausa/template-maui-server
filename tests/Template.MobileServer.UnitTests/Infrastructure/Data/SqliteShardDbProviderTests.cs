namespace Template.MobileServer.Infrastructure.Data;

using Microsoft.Data.Sqlite;

using Template.MobileServer.Web.Infrastructure.Data;

public sealed class SqliteShardDbProviderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "template-shard-tests", Guid.NewGuid().ToString("N"));

    private readonly SqliteShardDbProvider provider;

    public SqliteShardDbProviderTests()
    {
        Directory.CreateDirectory(root);
        provider = new SqliteShardDbProvider(new SqliteShardDbProviderOption { Root = root, CacheSize = 256 });
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }

    [Fact]
    public async Task OpenCreatesFileAndOpenExistingDoesNot()
    {
        // Act
        await using var created = await provider.OpenAsync("device-1", TestContext.Current.CancellationToken);
        await using var missing = await provider.OpenExistingAsync("device-2", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(missing);
        Assert.True(File.Exists(Path.Combine(root, "device-1.db")));
        Assert.False(File.Exists(Path.Combine(root, "device-2.db")));
        Assert.Equal(["device-1"], provider.EnumerateKeys());
    }

    [Fact]
    public async Task OpenAppliesPragma()
    {
        // Act
        await using var con = await provider.OpenAsync("device-1", TestContext.Current.CancellationToken);

        // Assert
        await using var command = con.CreateCommand();
        command.CommandText = "PRAGMA cache_size";
        Assert.Equal(-256L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        command.CommandText = "PRAGMA synchronous";
        Assert.Equal(1L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteRemovesDatabaseAndJournalFiles()
    {
        // Arrange
        var path = Path.Combine(root, "device-1.db");
        await using (var con = await provider.OpenAsync("device-1", TestContext.Current.CancellationToken))
        {
            await using var command = con.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = WAL; CREATE TABLE Item (Id INTEGER)";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        Assert.True(File.Exists(path + "-wal"));

        // Act
        var deleted = provider.Delete("device-1");
        var again = provider.Delete("device-1");

        // Assert
        Assert.True(deleted);
        Assert.False(again);
        Assert.False(File.Exists(path));
        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));
        Assert.Empty(provider.EnumerateKeys());
    }

    [Fact]
    public async Task EnumerateKeysSkipsInvalidNames()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(root, "device-1.db"), string.Empty, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "device 2.db"), string.Empty, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "device-3.txt"), string.Empty, TestContext.Current.CancellationToken);

        // Act
        var keys = provider.EnumerateKeys();

        // Assert
        Assert.Equal(["device-1"], keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../data")]
    [InlineData("device.1")]
    [InlineData("device 1")]
    public async Task InvalidKeyThrows(string key)
    {
        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await provider.OpenAsync(key, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(async () => await provider.OpenExistingAsync(key, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentException>(() => provider.Delete(key));
    }
}
