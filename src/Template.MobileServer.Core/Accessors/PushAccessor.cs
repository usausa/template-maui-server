namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class PushAccessor
{
    [Query]
    public partial ValueTask<List<PushMessageEntity>> InsertAsync(string? terminalId, string title, string body, DateTime createdAt);

    [Query]
    public partial ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string terminalId, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdateDeliveredAsync(long id, string terminalId, DateTime deliveredAt);

    [Execute]
    public partial ValueTask<int> DeleteBeforeAsync(DateTime before);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(DbTransaction tx, string terminalId);
}
