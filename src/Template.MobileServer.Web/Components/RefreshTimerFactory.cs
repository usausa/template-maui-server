namespace Template.MobileServer.Web.Components;

public sealed class RefreshTimerFactory
{
    private readonly ILogger<RefreshTimer> log;

    private readonly TimeProvider timeProvider;

    private readonly IHostApplicationLifetime lifetime;

    public RefreshTimerFactory(
        ILogger<RefreshTimer> log,
        TimeProvider timeProvider,
        IHostApplicationLifetime lifetime)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.lifetime = lifetime;
    }

    public RefreshTimer Create(TimeSpan interval, Func<Task> refresh) =>
        new(log, timeProvider, interval, refresh, lifetime.ApplicationStopping);
}
