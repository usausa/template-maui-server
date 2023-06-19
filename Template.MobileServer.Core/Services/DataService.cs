namespace Template.MobileServer.Services;

using Smart.Data;
using Smart.Data.Accessor;

using Template.MobileServer.Accessor;

public class DataService
{
    private readonly IDialect dialect;

    private readonly IDataAccessor dataAccessor;

    public DataService(
        IDialect dialect,
        IAccessorResolver<IDataAccessor> dataAccessor)
    {
        this.dialect = dialect;
        this.dataAccessor = dataAccessor.Accessor;
    }

    public ValueTask<int> CountAsync() =>
        dataAccessor.CountAsync();

    public ValueTask<DataEntity?> QueryAsync(int id) =>
        dataAccessor.QueryAsync(id);

    public ValueTask<List<DataEntity>> QueryListAsync() =>
        dataAccessor.QueryListAsync();

    public async ValueTask<bool> InsertAsync(DataEntity entity)
    {
        try
        {
            await dataAccessor.InsertAsync(entity).ConfigureAwait(false);
            return true;
        }
        catch (DbException ex)
        {
            if (dialect.IsDuplicate(ex))
            {
                return false;
            }

            throw;
        }
    }
}
