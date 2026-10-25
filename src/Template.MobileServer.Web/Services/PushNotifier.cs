namespace Template.MobileServer.Web.Services;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Hubs;

public sealed class PushNotifier
{
    private ILogger<PushNotifier> Log { get; }

    private IHubContext<PushHub, IPushClient> Hub { get; }

    private PushService PushService { get; }

    public event EventHandler? Changed;

    public PushNotifier(
        ILogger<PushNotifier> log,
        IHubContext<PushHub, IPushClient> hub,
        PushService pushService)
    {
        Log = log;
        Hub = hub;
        PushService = pushService;
    }

    public async ValueTask<List<PushMessageEntity>> SendAsync(string? terminalId, string title, string body)
    {
        var messages = await PushService.InsertAsync(terminalId, title, body);
        foreach (var message in messages)
        {
            await Hub.Clients.Group(message.TerminalId).Receive(message.ToPushMessage());
        }

        Log.InfoPushSent(terminalId ?? "*", messages.Count);

        if (messages.Count > 0)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return messages;
    }

    public async ValueTask<bool> AcknowledgeAsync(string terminalId, long id)
    {
        var delivered = await PushService.UpdateDeliveredAsync(terminalId, id);
        if (delivered)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return delivered;
    }
}
