namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Data;
using Template.MobileServer.Models;
using Template.MobileServer.Models.Entity;

public sealed class DataService
{
    // 並べ替えに使える列。SqlHelper.NormalizeSortがこの集合以外を弾く
    private static readonly string[] SortKeys = ["Name", "Value", "CreatedAt"];

    // 一致しなかったときの並び順。テーブルの主キー
    private const string DefaultSortColumn = "Id";

    private readonly IDialect dialect;

    private readonly DataAccessor dataAccessor;

    private readonly TimeProvider timeProvider;

    public DataService(
        IDialect dialect,
        DataAccessor dataAccessor,
        TimeProvider timeProvider)
    {
        this.dialect = dialect;
        this.dataAccessor = dataAccessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        dataAccessor.Create();

    public ValueTask<int> CountAsync(string? name, CancellationToken cancellationToken = default) =>
        dataAccessor.CountAsync(name, cancellationToken);

    // ページ番号と件数で扱い、総件数と合わせて返す
    public async ValueTask<PagedResult<DataEntity>> QueryPageAsync(string? name, string? sort, bool desc, int page, int size, CancellationToken cancellationToken = default)
    {
        var total = await dataAccessor.CountAsync(name, cancellationToken);
        var items = await dataAccessor.QueryPageAsync(name, SqlHelper.NormalizeSort(SortKeys, DefaultSortColumn, sort, desc), page * size, size, cancellationToken);
        return new PagedResult<DataEntity>(total, page, size, items);
    }

    public ValueTask<List<DataEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        dataAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<DataEntity?> QueryAsync(long id) =>
        dataAccessor.QueryAsync(id);

    public async ValueTask<long?> InsertAsync(string name, int value)
    {
        try
        {
            return await dataAccessor.InsertAsync(name, value, timeProvider.GetLocalNow().DateTime);
        }
        catch (DbException ex)
        {
            if (dialect.IsDuplicate(ex))
            {
                return null;
            }

            throw;
        }
    }

    public async ValueTask<DataWriteStatus> UpdateAsync(long id, string name, int value)
    {
        try
        {
            var rows = await dataAccessor.UpdateAsync(id, name, value);
            return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
        }
        catch (DbException ex)
        {
            if (dialect.IsDuplicate(ex))
            {
                return DataWriteStatus.Duplicate;
            }

            throw;
        }
    }

    public async ValueTask<bool> DeleteAsync(long id)
    {
        var rows = await dataAccessor.DeleteAsync(id);
        return rows > 0;
    }
}
