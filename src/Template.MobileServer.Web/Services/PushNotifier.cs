namespace Template.MobileServer.Web.Services;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Hubs;

// 端末への通知の送信と受け取りの記録。送信は行を作ってから、宛先の端末が接続中ならハブですぐに送る (未接続の端末には次の接続で届く)。
// 未達が変わったら Changed で知らせる (受け手は別のスレッドで呼ばれる)
public sealed class PushNotifier
{
    private readonly ILogger<PushNotifier> log;

    private readonly IHubContext<PushHub, IPushClient> hub;

    private readonly PushService pushService;

    public event EventHandler? Changed;

    public PushNotifier(
        ILogger<PushNotifier> log,
        IHubContext<PushHub, IPushClient> hub,
        PushService pushService)
    {
        this.log = log;
        this.hub = hub;
        this.pushService = pushService;
    }

    // 宛先の端末 ID (null は全端末)。作った行を返す (宛先の端末が無ければ空)
    public async ValueTask<List<PushMessageEntity>> SendAsync(string? deviceId, string title, string body, CancellationToken cancellationToken = default)
    {
        var messages = await pushService.InsertAsync(deviceId, title, body, cancellationToken);
        foreach (var message in messages)
        {
            await hub.Clients.Group(message.DeviceId).Receive(message.ToPushMessage());
        }

        log.InfoPushSent(deviceId ?? "*", messages.Count);

        if (messages.Count > 0)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return messages;
    }

    // 端末の受け取りの応答。届いた日時を記録したら true
    public async ValueTask<bool> AcknowledgeAsync(string deviceId, long id, CancellationToken cancellationToken = default)
    {
        var delivered = await pushService.UpdateDeliveredAsync(deviceId, id, cancellationToken);
        if (delivered)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return delivered;
    }
}
