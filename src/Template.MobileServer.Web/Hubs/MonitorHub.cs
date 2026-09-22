namespace Template.MobileServer.Web.Hubs;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Services;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

// Client -> Server
public sealed class DeviceStatusMessage
{
    public string DeviceId { get; set; } = default!;

    public string Model { get; set; } = default!;

    public string Platform { get; set; } = default!;

    public double Battery { get; set; }

    public string BatteryState { get; set; } = default!;

    public string Network { get; set; } = default!;
}

// Server -> Client
public sealed class ServerStatusMessage
{
    public DateTimeOffset Time { get; set; }

    public double CpuPercent { get; set; }

    public long WorkingSet { get; set; }

    public int Connections { get; set; }
}

// Server -> Client
public sealed class NotificationMessage
{
    public string Title { get; set; } = default!;

    public string Body { get; set; } = default!;

    public DateTimeOffset SentAt { get; set; }
}

//--------------------------------------------------------------------------------
// Connection
//--------------------------------------------------------------------------------

public interface IMonitorClient
{
    Task ServerStatus(ServerStatusMessage status);

    Task Notify(NotificationMessage notification);
}

public sealed class MonitorHub : Hub<IMonitorClient>
{
    private readonly ILogger<MonitorHub> log;

    private readonly TimeProvider timeProvider;

    private readonly DeviceRegistry registry;

    public MonitorHub(
        ILogger<MonitorHub> log,
        TimeProvider timeProvider,
        DeviceRegistry registry)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.registry = registry;
    }

    public override Task OnConnectedAsync()
    {
        registry.Register(Context.ConnectionId, timeProvider.GetUtcNow(), Context.Abort);

        log.InfoMonitorConnected(Context.ConnectionId, registry.Count);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        registry.Remove(Context.ConnectionId);

        log.InfoMonitorDisconnected(Context.ConnectionId, registry.Count, exception);

        return base.OnDisconnectedAsync(exception);
    }

    public void ReportDeviceStatus(DeviceStatusMessage status) =>
        registry.Update(Context.ConnectionId, status, timeProvider.GetUtcNow());
}
