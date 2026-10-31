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

    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    public ValueTask<List<PushMessageEntity>> QueryPendingListAsync(string terminalId, CancellationToken cancellationToken = default) =>
        PushAccessor.QueryPendingListAsync(terminalId, cancellationToken);

    public ValueTask<List<PushPendingSummaryView>> QueryPendingSummaryAsync(CancellationToken cancellationToken = default) =>
        PushAccessor.QueryPendingSummaryAsync(cancellationToken);

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    public ValueTask<List<PushMessageEntity>> InsertAsync(string? terminalId, string title, string body)
    {
        var context = ContextProvider.Current;

        return PushAccessor.InsertAsync(terminalId, title, body, context.Now.UtcDateTime);
    }

    public async ValueTask<bool> UpdateDeliveredAsync(string terminalId, long id)
    {
        var context = ContextProvider.Current;

        return await PushAccessor.UpdateDeliveredAsync(id, terminalId, context.Now.UtcDateTime) > 0;
    }

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    public ValueTask<int> DeleteBeforeAsync(DateTime before) =>
        PushAccessor.DeleteBeforeAsync(before);
}
