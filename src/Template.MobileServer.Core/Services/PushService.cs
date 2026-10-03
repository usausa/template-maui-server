namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

// 端末への通知 (data.db)。宛先の端末ごとに 1 行を作り、端末の受け取りの応答で届いた日時を記録する
public sealed class PushService
{
    private readonly PushAccessor pushAccessor;

    private readonly ServiceContextProvider contextProvider;

    public PushService(
        PushAccessor pushAccessor,
        ServiceContextProvider contextProvider)
    {
        this.pushAccessor = pushAccessor;
        this.contextProvider = contextProvider;
    }

    // 宛先 (null は全端末) の登録済みで有効な端末ごとに 1 行を作る。宛先の端末が無ければ空
    public ValueTask<List<PushMessageEntity>> InsertAsync(string? deviceId, string title, string body, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;

        return pushAccessor.InsertAsync(deviceId, title, body, context.Now.UtcDateTime, cancellationToken);
    }

    // 端末の未達 (古い順)
    public ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string deviceId, CancellationToken cancellationToken = default) =>
        pushAccessor.QueryPendingListAsync(deviceId, cancellationToken);

    // 端末ごとの未達の件数 (未達の無い端末は含まない)
    public ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken = default) =>
        pushAccessor.QueryPendingSummaryAsync(cancellationToken);

    // 受け取りの応答。未達のときだけ届いた日時を記録する (無い・届いた・他の端末宛ては false)
    public async ValueTask<bool> UpdateDeliveredAsync(string deviceId, long id, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;

        return await pushAccessor.UpdateDeliveredAsync(id, deviceId, context.Now.UtcDateTime, cancellationToken) > 0;
    }

    // 期限を過ぎた行を削除する (届いた / 届いていないにかかわらず)
    public ValueTask<int> DeleteBeforeAsync(DateTime before, CancellationToken cancellationToken = default) =>
        pushAccessor.DeleteBeforeAsync(before, cancellationToken);
}
