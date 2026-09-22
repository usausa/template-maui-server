namespace Template.MobileServer.Web.Services;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class MonitorNotifier : IDisposable
{
    private readonly ILogger<MonitorNotifier> log;

    private readonly IHubContext<MonitorHub, IMonitorClient> hub;

    private readonly TimeProvider timeProvider;

    private readonly NotificationBus bus;

    public MonitorNotifier(
        ILogger<MonitorNotifier> log,
        IHubContext<MonitorHub, IMonitorClient> hub,
        TimeProvider timeProvider,
        NotificationBus bus)
    {
        this.log = log;
        this.hub = hub;
        this.timeProvider = timeProvider;
        this.bus = bus;

        bus.Received += OnReceived;
    }

    public void Dispose()
    {
        bus.Received -= OnReceived;
    }

    public Task NotifyAllAsync(string title, string body) =>
        hub.Clients.All.Notify(CreateNotification(title, body));

    public Task NotifyAsync(string connectionId, string title, string body) =>
        hub.Clients.Client(connectionId).Notify(CreateNotification(title, body));

    private NotificationMessage CreateNotification(string title, string body) => new()
    {
        Title = title,
        Body = body,
        SentAt = timeProvider.GetLocalNow()
    };

    private void OnReceived(object? sender, NotificationEventArgs e)
    {
        _ = RelayAsync(e.Message);
    }

    private async Task RelayAsync(string message)
    {
        try
        {
            await NotifyAllAsync("Server", message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.ErrorNotifyFailed(ex);
        }
    }
}
