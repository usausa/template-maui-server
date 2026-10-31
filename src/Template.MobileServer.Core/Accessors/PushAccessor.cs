namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class PushAccessor
{
    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    [Query]
    public partial ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string terminalId, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    [Query]
    public partial ValueTask<List<PushMessageEntity>> InsertAsync(string? terminalId, string title, string body, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> UpdateDeliveredAsync(long id, string terminalId, DateTime deliveredAt);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(DbTransaction tx, string terminalId);

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> DeleteBeforeAsync(DateTime before);
}
