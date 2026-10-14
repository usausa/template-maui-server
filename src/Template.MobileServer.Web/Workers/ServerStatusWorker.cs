namespace Template.MobileServer.Web.Workers;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Services;

public sealed class ServerStatusWorker : BackgroundService
{
    private ILogger<ServerStatusWorker> Log { get; }

    private TimeProvider TimeProvider { get; }

    private ServerStatusWorkerOption Options { get; }

    private IHubContext<MonitorHub, IMonitorClient> Hub { get; }

    private DeviceRegistry Registry { get; }

    public ServerStatusWorker(
        ILogger<ServerStatusWorker> log,
        TimeProvider timeProvider,
        ServerStatusWorkerOption options,
        IHubContext<MonitorHub, IMonitorClient> hub,
        DeviceRegistry registry)
    {
        Log = log;
        TimeProvider = timeProvider;
        Options = options;
        Hub = hub;
        Registry = registry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.InfoWorkerStart(nameof(ServerStatusWorker));

        try
        {
            var lastCpuTime = Environment.CpuUsage.TotalTime;
            var lastTimestamp = TimeProvider.GetTimestamp();

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Options.Interval), TimeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Process CPU time against the elapsed time, one core = 100% (clamped when several threads are busy)
                var cpuTime = Environment.CpuUsage.TotalTime;
                var timestamp = TimeProvider.GetTimestamp();
                var elapsed = TimeProvider.GetElapsedTime(lastTimestamp, timestamp);
                var cpu = elapsed > TimeSpan.Zero
                    ? Math.Clamp((cpuTime - lastCpuTime) / elapsed * 100, 0, 100)
                    : 0;
                lastCpuTime = cpuTime;
                lastTimestamp = timestamp;

                if (Registry.Count == 0)
                {
                    continue;
                }

                try
                {
                    await Hub.Clients.All.ServerStatus(new ServerStatusMessage
                    {
                        Time = TimeProvider.GetLocalNow(),
                        CpuPercent = Math.Round(cpu, 1),
                        WorkingSet = Environment.WorkingSet,
                        Connections = Registry.Count
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.ErrorWorkerException(nameof(ServerStatusWorker), ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            Log.InfoWorkerStop(nameof(ServerStatusWorker));
        }
    }
}
