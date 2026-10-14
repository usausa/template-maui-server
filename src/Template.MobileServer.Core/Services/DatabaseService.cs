namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

public sealed class DatabaseService
{
    private IDbProvider Provider { get; }

    private GenericAccessor GenericAccessor { get; }

    public DatabaseService(
        IDbProvider provider,
        GenericAccessor genericAccessor)
    {
        Provider = provider;
        GenericAccessor = genericAccessor;
    }

    public async ValueTask InitializeAsync(string schemaPath)
    {
        var schema = await File.ReadAllTextAsync(schemaPath);
        await Provider.UsingAsync(con => GenericAccessor.ExecuteSchemaAsync(con, schema));
    }
}
