namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class PushAccessor
{
    [Query]
    public partial ValueTask<List<PushMessageEntity>> InsertAsync(string? deviceId, string title, string body, DateTime createdAt);

    [Query]
    public partial ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string deviceId, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdateDeliveredAsync(long id, string deviceId, DateTime deliveredAt);

    [Execute]
    public partial ValueTask<int> DeleteBeforeAsync(DateTime before);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(DbTransaction tx, string deviceId);
}
