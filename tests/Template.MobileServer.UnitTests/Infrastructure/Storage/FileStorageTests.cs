namespace Template.MobileServer.Infrastructure.Storage;

public sealed class FileStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "template-storage-tests", Guid.NewGuid().ToString("N"));

    private readonly string outside = Path.Combine(Path.GetTempPath(), "template-storage-tests", Guid.NewGuid().ToString("N"));

    public FileStorageTests()
    {
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
    }

    public void Dispose()
    {
        Directory.Delete(root, true);
        Directory.Delete(outside, true);
    }

    // root の下のパスは使える
    [Fact]
    public async Task WriteAndReadInsideRoot()
    {
        // Arrange
        var storage = new FileStorage(new FileStorageOption { Root = root });
        await using var source = new MemoryStream("data"u8.ToArray());

        // Act
        await storage.WriteAsync("dir/file.txt", source, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(await storage.FileExistsAsync("dir/file.txt", TestContext.Current.CancellationToken));
    }

    // root の外へ出るパスは使えない
    [Fact]
    public async Task RejectsPathOutsideRoot()
    {
        // Arrange
        var storage = new FileStorage(new FileStorageOption { Root = root });

        // Act / Assert
        await Assert.ThrowsAsync<StorageException>(async () => await storage.FileExistsAsync("../file.txt", TestContext.Current.CancellationToken));
    }

    // root の下のシンボリックリンクを経由するパスは使えない (リンクを作れない環境では飛ばす)
    [Fact]
    public async Task RejectsPathThroughSymbolicLink()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.txt"), "secret", TestContext.Current.CancellationToken);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(root, "link"), outside);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Assert.Skip("Symbolic links are not available.");
        }

        var storage = new FileStorage(new FileStorageOption { Root = root });

        // Act / Assert
        await Assert.ThrowsAsync<StorageException>(async () => await storage.ReadAsync("link/secret.txt", TestContext.Current.CancellationToken));
    }
}
