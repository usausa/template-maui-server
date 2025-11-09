namespace Template.MobileServer.Accessor;

[DataAccessor]
public interface IDataAccessor
{
    [Execute]
    void Create();

    [ExecuteScalar]
    ValueTask<int> CountAsync();

    [QueryFirstOrDefault]
    ValueTask<DataEntity?> QueryAsync(int id);

    [Query]
    ValueTask<List<DataEntity>> QueryListAsync();

    [Insert]
    ValueTask<int> InsertAsync(DataEntity entity);
}
