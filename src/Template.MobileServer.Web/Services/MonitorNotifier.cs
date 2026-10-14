namespace Template.MobileServer.Web.Services;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class MonitorNotifier : IDisposable
{
    private ILogger<MonitorNotifier> Log { get; }

    private TimeProvider TimeProvider { get; }

    private IHubContext<MonitorHub, IMonitorClient> Hub { get; }

    private NotificationBus Bus { get; }

    public MonitorNotifier(
        ILogger<MonitorNotifier> log,
        TimeProvider timeProvider,
        IHubContext<MonitorHub, IMonitorClient> hub,
        NotificationBus bus)
    {
        Log = log;
        TimeProvider = timeProvider;
        Hub = hub;
        Bus = bus;

        bus.Received += OnReceived;
    }

    public void Dispose()
    {
        Bus.Received -= OnReceived;
    }

    public Task NotifyAllAsync(string title, string body) =>
        Hub.Clients.All.Notify(CreateNotification(title, body));

    public Task NotifyAsync(string connectionId, string title, string body) =>
        Hub.Clients.Client(connectionId).Notify(CreateNotification(title, body));

    private NotificationMessage CreateNotification(string title, string body) => new()
    {
        Title = title,
        Body = body,
        SentAt = TimeProvider.GetLocalNow()
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
            Log.ErrorNotifyFailed(ex);
        }
    }
}
