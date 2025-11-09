namespace Template.MobileServer.Components.Storage;

public sealed class FileStorage : IStorage
{
    private const int CopyBufferSize = 81920;

    private readonly string root;

    public FileStorage(FileStorageOptions options)
    {
        root = Path.GetFullPath(options.Root);
    }

    private string NormalizePath(string path)
    {
        if (path.EndsWith('/'))
        {
            path = path[..^1];
        }

        var fullPath = Path.Combine(root, path);
        if (fullPath.Length < root.Length)
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

    public ValueTask<string[]> ListAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
#pragma warning disable CS8619
        return ValueTask.FromResult(Directory.GetDirectories(path).Concat(Directory.GetFiles(path)).Select(Path.GetFileName).ToArray());
#pragma warning restore CS8619
    }

    public ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        path = NormalizePath(path);
        File.Delete(path);

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

#pragma warning disable CA2007
        await using var fs = File.OpenWrite(path);
#pragma warning restore CA2007
        await stream.CopyToAsync(fs, CopyBufferSize, cancellationToken).ConfigureAwait(false);
    }
}
