namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Models;
using Template.MobileServer.Models.Entity;

public sealed class DataService
{
    private IDialect Dialect { get; }

    private DataAccessor DataAccessor { get; }

    private ServiceContextProvider ContextProvider { get; }

    public DataService(
        IDialect dialect,
        DataAccessor dataAccessor,
        ServiceContextProvider contextProvider)
    {
        Dialect = dialect;
        DataAccessor = dataAccessor;
        ContextProvider = contextProvider;
    }

    public ValueTask<int> CountAsync(string? name, CancellationToken cancellationToken = default) =>
        DataAccessor.CountAsync(Dialect.Match(name), cancellationToken);

    public async ValueTask<PagedResult<DataEntity>> QueryPageAsync(string? name, DataSort sort, bool desc, int page, int size, CancellationToken cancellationToken = default)
    {
        var pattern = Dialect.Match(name);
        var total = await DataAccessor.CountAsync(pattern, cancellationToken);
        var items = await DataAccessor.QueryPageAsync(pattern, sort, desc, size, page * size, cancellationToken);
        return new PagedResult<DataEntity>(total, page, size, items);
    }

    public async ValueTask<RangeResult<DataEntity>> QueryRangeAsync(int offset, int size, CancellationToken cancellationToken = default)
    {
        var total = await DataAccessor.CountAsync(null, cancellationToken);
        var items = await DataAccessor.QueryPageAsync(null, DataSort.Id, false, size, offset, cancellationToken);
        return new RangeResult<DataEntity>(total, offset, size, items);
    }

    public ValueTask<List<DataEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        DataAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<DataEntity?> QueryAsync(long id) =>
        DataAccessor.QueryAsync(id);

    public async ValueTask<DataWriteStatus> InsertAsync(DataEntity entity)
    {
        var context = ContextProvider.Current;

        try
        {
            entity.CreatedAt = context.Now.UtcDateTime;
            entity.Id = await DataAccessor.InsertAsync(entity.Name, entity.Value, entity.CreatedAt);
            return DataWriteStatus.Success;
        }
        catch (DbException ex) when (Dialect.IsDuplicate(ex))
        {
            return DataWriteStatus.Duplicate;
        }
    }

    public async ValueTask<DataWriteStatus> UpdateAsync(long id, string name, int value)
    {
        try
        {
            var rows = await DataAccessor.UpdateAsync(id, name, value);
            return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
        }
        catch (DbException ex) when (Dialect.IsDuplicate(ex))
        {
            return DataWriteStatus.Duplicate;
        }
    }

    public async ValueTask<DataWriteStatus> DeleteAsync(long id) =>
        await DataAccessor.DeleteAsync(id) > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
}
