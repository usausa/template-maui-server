namespace Template.MobileServer.Web.Hubs;

using Microsoft.AspNetCore.SignalR;

using Smart.Mapper;

using Template.MobileServer.Web.Services;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

// Server -> Client
public sealed class PushMessage
{
    public long Id { get; set; }

    public string Title { get; set; } = default!;

    public string Body { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}

//--------------------------------------------------------------------------------
// Mapper
//--------------------------------------------------------------------------------

public static partial class PushMapper
{
    [Mapper]
    public static partial PushMessage ToPushMessage(this PushMessageEntity entity);
}

//--------------------------------------------------------------------------------
// Connection
//--------------------------------------------------------------------------------

public interface IPushClient
{
    Task Receive(PushMessage message);
}

public sealed class PushHub : Hub<IPushClient>
{
    public const string TerminalIdParameter = "terminalId";

    private const string TerminalIdKey = "TerminalId";

    private ILogger<PushHub> Log { get; }

    private PushService PushService { get; }

    private PushNotifier Notifier { get; }

    public PushHub(
        ILogger<PushHub> log,
        PushService pushService,
        PushNotifier notifier)
    {
        Log = log;
        PushService = pushService;
        Notifier = notifier;
    }

    public override async Task OnConnectedAsync()
    {
        var terminalId = Context.GetHttpContext()?.Request.Query[TerminalIdParameter].ToString();
        if (!TerminalIdFormat.IsValid(terminalId))
        {
            Log.WarnPushRejected(Context.ConnectionId, terminalId);
            Context.Abort();
            return;
        }

        Context.Items[TerminalIdKey] = terminalId;

        await Groups.AddToGroupAsync(Context.ConnectionId, terminalId, Context.ConnectionAborted);
        var messages = await PushService.QueryPendingListAsync(terminalId, Context.ConnectionAborted);
        foreach (var message in messages)
        {
            await Clients.Caller.Receive(message.ToPushMessage());
        }

        Log.InfoPushConnected(Context.ConnectionId, terminalId, messages.Count);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Log.IsEnabled(LogLevel.Information))
        {
            Log.InfoPushDisconnected(Context.ConnectionId, GetTerminalId(), exception);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task Acknowledge(long id)
    {
        if (GetTerminalId() is { } terminalId)
        {
            await Notifier.AcknowledgeAsync(terminalId, id);
        }
    }

    private string? GetTerminalId() =>
        Context.Items.TryGetValue(TerminalIdKey, out var value) ? value as string : null;
}
