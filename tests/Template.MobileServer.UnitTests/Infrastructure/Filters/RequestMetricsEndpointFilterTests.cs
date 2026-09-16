namespace Template.MobileServer.Infrastructure.Filters;

using System.Diagnostics.Metrics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Template.MobileServer.Web.Application.Telemetry;
using Template.MobileServer.Web.Infrastructure.Filters;
using Template.MobileServer.Web.Settings;

public sealed class RequestMetricsEndpointFilterTests
{
    [Fact]
    public async Task InvokeCountsRequestWithMethodAndRoute()
    {
        // Arrange
        await using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var instrument = new ApplicationInstrument(provider.GetRequiredService<IMeterFactory>());
        using var listener = CreateListener("api.request.execution", out var measurements);
        var filter = new RequestMetricsEndpointFilter(instrument, TimeProvider.System, new TelemetrySetting { LongExecutionThreshold = 3000 }, NullLogger<RequestMetricsEndpointFilter>.Instance);

        // Act
        var result = await filter.InvokeAsync(CreateContext("GET", "/api/data"), static _ => ValueTask.FromResult<object?>("ok"));

        // Assert
        Assert.Equal("ok", result);
        var measurement = Assert.Single(measurements);
        Assert.Contains(new KeyValuePair<string, object?>("http.request.method", "GET"), measurement);
        Assert.Contains(new KeyValuePair<string, object?>("http.route", "/api/data"), measurement);
    }

    [Fact]
    public async Task InvokeCountsLongExecutionOverThreshold()
    {
        // Arrange
        await using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var instrument = new ApplicationInstrument(provider.GetRequiredService<IMeterFactory>());
        using var listener = CreateListener("api.request.long.execution", out var measurements);
        var timeProvider = Substitute.ForPartsOf<TimeProvider>();
        timeProvider.GetTimestamp().Returns(0L, TimeProvider.System.TimestampFrequency * 5);
        var filter = new RequestMetricsEndpointFilter(instrument, timeProvider, new TelemetrySetting { LongExecutionThreshold = 3000 }, NullLogger<RequestMetricsEndpointFilter>.Instance);

        // Act
        await filter.InvokeAsync(CreateContext("POST", "/api/data"), static _ => ValueTask.FromResult<object?>(null));

        // Assert
        var measurement = Assert.Single(measurements);
        Assert.Contains(new KeyValuePair<string, object?>("http.route", "/api/data"), measurement);
    }

    private static MeterListener CreateListener(string instrumentName, out List<KeyValuePair<string, object?>[]> measurements)
    {
        var list = new List<KeyValuePair<string, object?>[]>();
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if ((instrument.Meter.Name == Source.Name) && (instrument.Name == instrumentName))
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => list.Add(tags.ToArray()));
        listener.Start();
        measurements = list;
        return listener;
    }

    private static DefaultEndpointFilterInvocationContext CreateContext(string method, string route)
    {
        var httpContext = new DefaultHttpContext { Request = { Method = method } };
        httpContext.SetEndpoint(new RouteEndpoint(static _ => Task.CompletedTask, RoutePatternFactory.Parse(route), 0, null, null));
        return new DefaultEndpointFilterInvocationContext(httpContext);
    }
}
