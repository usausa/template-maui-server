namespace Template.MobileServer.Web.Application.Context;

using Microsoft.AspNetCore.SignalR;

public sealed class ServiceContextHubFilter : IHubFilter
{
    private TimeProvider TimeProvider { get; }

    private ApplicationServiceContextProvider Provider { get; }

    public ServiceContextHubFilter(
        TimeProvider timeProvider,
        ApplicationServiceContextProvider provider)
    {
        TimeProvider = timeProvider;
        Provider = provider;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        using var scope = Provider.Begin(() => new ServiceContext(TimeProvider.GetLocalNow(), HttpServiceContext.GetUserId(invocationContext.Context.User)));
        return await next(invocationContext);
    }
}
