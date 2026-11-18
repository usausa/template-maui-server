namespace Template.MobileServer.Web.Application.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public sealed class ApplicationInstrument : IDisposable
{
    private readonly TimeProvider timeProvider;

    private readonly long startTimestamp;

    private readonly Meter meter;

    private readonly Counter<long> requestLongExecution;

    private readonly Counter<long> requestExecution;

    public ActivitySource ActivitySource { get; }

    public ApplicationInstrument(TimeProvider timeProvider, IMeterFactory meterFactory)
    {
        this.timeProvider = timeProvider;
        startTimestamp = timeProvider.GetTimestamp();

        ActivitySource = new ActivitySource(Source.Name, Source.Version);
        meter = meterFactory.Create(Source.Name, Source.Version);

        meter.CreateObservableCounter("application.uptime", ObserveApplicationUptime);

        requestLongExecution = meter.CreateCounter<long>("api.request.long.execution", description: "Long execution count.");
        requestExecution = meter.CreateCounter<long>("api.request.execution", description: "API request count");
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        meter.Dispose();
    }

    private long ObserveApplicationUptime() =>
        (long)timeProvider.GetElapsedTime(startTimestamp).TotalSeconds;

    public void IncrementRequestExecution(string method, string route) =>
        requestExecution.Add(1, new("http.request.method", method), new("http.route", route));

    public void IncrementRequestLongExecution(string method, string route) =>
        requestLongExecution.Add(1, new("http.request.method", method), new("http.route", route));
}
