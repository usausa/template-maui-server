namespace Template.MobileServer.Accessors;

[DataAccessor]
public sealed partial class GenericAccessor
{
    [DirectSql]
    [Execute]
    public partial ValueTask<int> ExecuteSchemaAsync(DbConnection con, string sql, CancellationToken cancellationToken);
}
