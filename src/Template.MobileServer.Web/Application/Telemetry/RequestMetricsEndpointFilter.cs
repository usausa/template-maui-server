namespace Template.MobileServer.Web.Application.Telemetry;

public sealed class RequestMetricsEndpointFilter : IEndpointFilter
{
    private ILogger<RequestMetricsEndpointFilter> Log { get; }

    private TimeProvider TimeProvider { get; }

    private ApplicationInstrument Instrument { get; }

    private readonly TimeSpan longExecutionThreshold;

    public RequestMetricsEndpointFilter(
        ILogger<RequestMetricsEndpointFilter> log,
        TimeProvider timeProvider,
        TelemetrySetting setting,
        ApplicationInstrument instrument)
    {
        Log = log;
        TimeProvider = timeProvider;
        Instrument = instrument;
        longExecutionThreshold = TimeSpan.FromMilliseconds(setting.LongExecutionThreshold);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        var route = (context.HttpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.HttpContext.Request.Path.Value ?? string.Empty;
        Instrument.IncrementRequestExecution(method, route);

        var start = TimeProvider.GetTimestamp();
        try
        {
            return await next(context);
        }
        finally
        {
            var elapsed = TimeProvider.GetElapsedTime(start);
            if (elapsed >= longExecutionThreshold)
            {
                Instrument.IncrementRequestLongExecution(method, route);
                Log.WarnLongExecution(method, route, (long)elapsed.TotalMilliseconds);
            }
        }
    }
}
