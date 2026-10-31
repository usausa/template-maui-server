namespace Template.MobileServer.Accessors;

[DataAccessor]
public sealed partial class GenericAccessor
{
    //--------------------------------------------------------------------------------
    // Schema
    //--------------------------------------------------------------------------------

    [DirectSql]
    [Execute]
    public partial ValueTask<int> ExecuteSchemaAsync(DbConnection con, string sql);

    [ExecuteScalar]
    public partial ValueTask<long> QueryUserVersionAsync(DbConnection con);
}
