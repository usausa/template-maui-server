namespace Template.MobileServer.Infrastructure.Storage;

public sealed record StorageEntry(string Name, bool IsDirectory, long Size, DateTime LastModified);
