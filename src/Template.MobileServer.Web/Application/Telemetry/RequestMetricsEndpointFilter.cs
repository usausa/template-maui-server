namespace Template.MobileServer.Web.Application.Telemetry;

public sealed class RequestMetricsEndpointFilter : IEndpointFilter
{
    private readonly ApplicationInstrument instrument;

    private readonly TimeProvider timeProvider;

    private readonly ILogger<RequestMetricsEndpointFilter> log;

    private readonly TimeSpan longExecutionThreshold;

    public RequestMetricsEndpointFilter(
        ApplicationInstrument instrument,
        TimeProvider timeProvider,
        TelemetrySetting setting,
        ILogger<RequestMetricsEndpointFilter> log)
    {
        this.instrument = instrument;
        this.timeProvider = timeProvider;
        this.log = log;
        longExecutionThreshold = TimeSpan.FromMilliseconds(setting.LongExecutionThreshold);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        var route = (context.HttpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.HttpContext.Request.Path.Value ?? string.Empty;
        instrument.IncrementRequestExecution(method, route);

        var start = timeProvider.GetTimestamp();
        try
        {
            return await next(context);
        }
        finally
        {
            var elapsed = timeProvider.GetElapsedTime(start);
            if (elapsed >= longExecutionThreshold)
            {
                instrument.IncrementRequestLongExecution(method, route);
                log.WarnLongExecution(method, route, (long)elapsed.TotalMilliseconds);
            }
        }
    }
}
