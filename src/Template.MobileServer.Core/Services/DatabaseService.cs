namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

public sealed class DatabaseService
{
    private readonly IDbProvider provider;

    private readonly GenericAccessor genericAccessor;

    public DatabaseService(
        IDbProvider provider,
        GenericAccessor genericAccessor)
    {
        this.provider = provider;
        this.genericAccessor = genericAccessor;
    }

    public async ValueTask InitializeAsync(string schemaPath, CancellationToken cancellationToken = default)
    {
        var schema = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        await provider.UsingAsync(con => genericAccessor.ExecuteSchemaAsync(con, schema, cancellationToken), cancellationToken);
    }
}
