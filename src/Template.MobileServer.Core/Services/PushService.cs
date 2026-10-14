namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

// 端末への通知 (data.db)。宛先の端末ごとに 1 行を作り、端末の受け取りの応答で届いた日時を記録する
public sealed class PushService
{
    private PushAccessor PushAccessor { get; }

    private ServiceContextProvider ContextProvider { get; }

    public PushService(
        PushAccessor pushAccessor,
        ServiceContextProvider contextProvider)
    {
        PushAccessor = pushAccessor;
        ContextProvider = contextProvider;
    }

    // 宛先 (null は全端末) の登録済みで有効な端末ごとに 1 行を作る。宛先の端末が無ければ空
    public ValueTask<List<PushMessageEntity>> InsertAsync(string? deviceId, string title, string body)
    {
        var context = ContextProvider.Current;

        return PushAccessor.InsertAsync(deviceId, title, body, context.Now.UtcDateTime);
    }

    // 端末の未達 (古い順)
    public ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string deviceId, CancellationToken cancellationToken = default) =>
        PushAccessor.QueryPendingListAsync(deviceId, cancellationToken);

    // 端末ごとの未達の件数 (未達の無い端末は含まない)
    public ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken = default) =>
        PushAccessor.QueryPendingSummaryAsync(cancellationToken);

    // 受け取りの応答。未達のときだけ届いた日時を記録する (無い・届いた・他の端末宛ては false)
    public async ValueTask<bool> UpdateDeliveredAsync(string deviceId, long id)
    {
        var context = ContextProvider.Current;

        return await PushAccessor.UpdateDeliveredAsync(id, deviceId, context.Now.UtcDateTime) > 0;
    }

    // 期限を過ぎた行を削除する (届いた / 届いていないにかかわらず)
    public ValueTask<int> DeleteBeforeAsync(DateTime before) =>
        PushAccessor.DeleteBeforeAsync(before);
}
