namespace Template.MobileServer.Web.Infrastructure.Data;

public sealed class SqliteShardDbProviderOption
{
    public string Root { get; set; } = default!;

    public int CacheSize { get; set; }
}
