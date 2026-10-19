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
    public const string DeviceIdParameter = "deviceId";

    private const string DeviceIdKey = "DeviceId";

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
        var deviceId = Context.GetHttpContext()?.Request.Query[DeviceIdParameter].ToString();
        if (!DeviceIdFormat.IsValid(deviceId))
        {
            Log.WarnPushRejected(Context.ConnectionId, deviceId);
            Context.Abort();
            return;
        }

        Context.Items[DeviceIdKey] = deviceId;

        await Groups.AddToGroupAsync(Context.ConnectionId, deviceId, Context.ConnectionAborted);
        var messages = await PushService.QueryPendingListAsync(deviceId, Context.ConnectionAborted);
        foreach (var message in messages)
        {
            await Clients.Caller.Receive(message.ToPushMessage());
        }

        Log.InfoPushConnected(Context.ConnectionId, deviceId, messages.Count);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Log.IsEnabled(LogLevel.Information))
        {
            Log.InfoPushDisconnected(Context.ConnectionId, GetDeviceId(), exception);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task Acknowledge(long id)
    {
        if (GetDeviceId() is { } deviceId)
        {
            await Notifier.AcknowledgeAsync(deviceId, id);
        }
    }

    private string? GetDeviceId() =>
        Context.Items.TryGetValue(DeviceIdKey, out var value) ? value as string : null;
}
