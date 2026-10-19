namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

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

    public ValueTask<List<PushMessageEntity>> InsertAsync(string? deviceId, string title, string body)
    {
        var context = ContextProvider.Current;

        return PushAccessor.InsertAsync(deviceId, title, body, context.Now.UtcDateTime);
    }

    public ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string deviceId, CancellationToken cancellationToken = default) =>
        PushAccessor.QueryPendingListAsync(deviceId, cancellationToken);

    public ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken = default) =>
        PushAccessor.QueryPendingSummaryAsync(cancellationToken);

    public async ValueTask<bool> UpdateDeliveredAsync(string deviceId, long id)
    {
        var context = ContextProvider.Current;

        return await PushAccessor.UpdateDeliveredAsync(id, deviceId, context.Now.UtcDateTime) > 0;
    }

    public ValueTask<int> DeleteBeforeAsync(DateTime before) =>
        PushAccessor.DeleteBeforeAsync(before);
}
