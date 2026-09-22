namespace Template.MobileServer.Web.Application.Context;

using Microsoft.AspNetCore.SignalR;

public sealed class ServiceContextHubFilter : IHubFilter
{
    private readonly TimeProvider timeProvider;

    private readonly ApplicationServiceContextProvider provider;

    public ServiceContextHubFilter(
        TimeProvider timeProvider,
        ApplicationServiceContextProvider provider)
    {
        this.timeProvider = timeProvider;
        this.provider = provider;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        using var scope = provider.Begin(() => new ServiceContext(timeProvider.GetLocalNow(), HttpServiceContext.GetUserId(invocationContext.Context.User)));
        return await next(invocationContext);
    }
}
