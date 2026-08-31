namespace Template.MobileServer.Accessors;

[DataAccessor]
public sealed partial class DataAccessor
{
    [Execute]
    public partial void Create();

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? name);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryPageAsync(string? name, int offset, int size);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryAllAsync();

    [QueryFirst]
    public partial ValueTask<DataEntity?> QueryAsync(long id);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, int value, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> UpdateAsync(long id, string name, int value);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long id);
}
