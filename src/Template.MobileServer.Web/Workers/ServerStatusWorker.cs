namespace Template.MobileServer.Web.Workers;

using Microsoft.AspNetCore.SignalR;

using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Services;

public sealed class ServerStatusWorker : BackgroundService
{
    private readonly ILogger<ServerStatusWorker> log;

    private readonly TimeProvider timeProvider;

    private readonly ServerStatusWorkerOption options;

    private readonly IHubContext<MonitorHub, IMonitorClient> hub;

    private readonly DeviceRegistry registry;

    public ServerStatusWorker(
        ILogger<ServerStatusWorker> log,
        TimeProvider timeProvider,
        ServerStatusWorkerOption options,
        IHubContext<MonitorHub, IMonitorClient> hub,
        DeviceRegistry registry)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.options = options;
        this.hub = hub;
        this.registry = registry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.InfoWorkerStart(nameof(ServerStatusWorker));

        try
        {
            var lastCpuTime = Environment.CpuUsage.TotalTime;
            var lastTimestamp = timeProvider.GetTimestamp();

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Interval), timeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Process CPU time against the elapsed time, one core = 100% (clamped when several threads are busy)
                var cpuTime = Environment.CpuUsage.TotalTime;
                var timestamp = timeProvider.GetTimestamp();
                var elapsed = timeProvider.GetElapsedTime(lastTimestamp, timestamp);
                var cpu = elapsed > TimeSpan.Zero
                    ? Math.Clamp((cpuTime - lastCpuTime) / elapsed * 100, 0, 100)
                    : 0;
                lastCpuTime = cpuTime;
                lastTimestamp = timestamp;

                if (registry.Count == 0)
                {
                    continue;
                }

                try
                {
                    await hub.Clients.All.ServerStatus(new ServerStatusMessage
                    {
                        Time = timeProvider.GetLocalNow(),
                        CpuPercent = Math.Round(cpu, 1),
                        WorkingSet = Environment.WorkingSet,
                        Connections = registry.Count
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.ErrorWorkerException(nameof(ServerStatusWorker), ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            log.InfoWorkerStop(nameof(ServerStatusWorker));
        }
    }
}
