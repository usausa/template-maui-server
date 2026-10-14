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

    // 送った日時 (UTC)
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

// 端末は接続の URL のクエリ (deviceId) で端末 ID を渡す。接続を端末 ID のグループに入れ、未達の通知を古い順に送る。
// 端末は表示の後に Acknowledge を呼ぶ (応答の無いまま切れた通知は次の接続で送り直すので、端末は同じ Id を 2 回出さない)
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

        // Join the group before reading, so that a message sent meanwhile is not lost (the device ignores the duplicate)
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

    // 受け取りの応答 (端末は表示の後に呼ぶ)
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
