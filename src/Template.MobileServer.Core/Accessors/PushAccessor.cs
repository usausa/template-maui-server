namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class PushAccessor
{
    // 宛先 (null は全端末) の登録済みで有効な端末ごとに 1 行を作り、作った行を返す
    [Query]
    public partial ValueTask<List<PushMessageEntity>> InsertAsync(string? deviceId, string title, string body, DateTime createdAt, CancellationToken cancellationToken);

    // 端末の未達 (古い順)
    [Query]
    public partial ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string deviceId, CancellationToken cancellationToken);

    // 端末ごとの未達の件数 (未達の無い端末は含まない)
    [Query]
    public partial ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken);

    // 未達のときだけ届いた日時を入れる (無い・届いた・他の端末宛ては 0 件)
    [Execute]
    public partial ValueTask<int> UpdateDeliveredAsync(long id, string deviceId, DateTime deliveredAt, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> DeleteBeforeAsync(DateTime before, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(string deviceId, CancellationToken cancellationToken);
}
