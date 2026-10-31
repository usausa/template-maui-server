namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class DataAccessor
{
    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? name, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryPageAsync(string? name, DataSort sort, bool desc, int limit, int offset, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryAllAsync(CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<DataEntity?> QueryAsync(long id);

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, int value, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> UpdateAsync(DataEntity entity);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long id);
}
