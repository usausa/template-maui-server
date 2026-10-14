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
    private ILogger<MonitorHub> Log { get; }

    private TimeProvider TimeProvider { get; }

    private DeviceRegistry Registry { get; }

    public MonitorHub(
        ILogger<MonitorHub> log,
        TimeProvider timeProvider,
        DeviceRegistry registry)
    {
        Log = log;
        TimeProvider = timeProvider;
        Registry = registry;
    }

    public override Task OnConnectedAsync()
    {
        Registry.Register(Context.ConnectionId, TimeProvider.GetUtcNow(), Context.Abort);

        Log.InfoMonitorConnected(Context.ConnectionId, Registry.Count);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Registry.Remove(Context.ConnectionId);

        Log.InfoMonitorDisconnected(Context.ConnectionId, Registry.Count, exception);

        return base.OnDisconnectedAsync(exception);
    }

    public void ReportDeviceStatus(DeviceStatusMessage status) =>
        Registry.Update(Context.ConnectionId, status, TimeProvider.GetUtcNow());
}
