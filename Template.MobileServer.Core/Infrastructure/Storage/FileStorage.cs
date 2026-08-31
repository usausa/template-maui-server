namespace Template.MobileServer.Infrastructure.Storage;

public sealed class FileStorage : IStorage
{
    private const int CopyBufferSize = 81920;

    private readonly string root;

    public FileStorage(FileStorageOptions options)
    {
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Root));
    }

    private string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, path));
        if ((fullPath != root) && !fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new StorageException("Invalid path.");
        }

        return fullPath;
    }

    public ValueTask<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
        return ValueTask.FromResult(File.Exists(path));
    }

    public ValueTask<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
        return ValueTask.FromResult(Directory.Exists(path));
    }

    public ValueTask<List<StorageEntry>> ListEntriesAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);

        var directory = new DirectoryInfo(path);
        var entries = new List<StorageEntry>();
        foreach (var info in directory.EnumerateDirectories())
        {
            entries.Add(new StorageEntry(info.Name, true, 0, info.LastWriteTime));
        }

        foreach (var info in directory.EnumerateFiles())
        {
            entries.Add(new StorageEntry(info.Name, false, info.Length, info.LastWriteTime));
        }

        return ValueTask.FromResult(entries);
    }

    public ValueTask CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
        Directory.CreateDirectory(path);

        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
        if (Directory.Exists(path))
        {
            // ディレクトリは再帰削除
            Directory.Delete(path, recursive: true);
        }
        else
        {
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<Stream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
#pragma warning disable CA2000
        return ValueTask.FromResult((Stream)File.OpenRead(path));
#pragma warning restore CA2000
    }

    public async ValueTask WriteAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var fs = File.Create(path);
        await stream.CopyToAsync(fs, CopyBufferSize, cancellationToken);
    }
}
