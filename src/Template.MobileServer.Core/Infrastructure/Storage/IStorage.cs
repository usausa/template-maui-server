namespace Template.MobileServer.Infrastructure.Storage;

public interface IStorage
{
    ValueTask<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

    ValueTask<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

    ValueTask<List<StorageEntry>> ListEntriesAsync(string path, CancellationToken cancellationToken = default);

    ValueTask CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);

    // ディレクトリを指定した場合は再帰削除
    ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default);

    ValueTask<Stream> ReadAsync(string path, CancellationToken cancellationToken = default);

    ValueTask WriteAsync(string path, Stream stream, CancellationToken cancellationToken = default);
}
