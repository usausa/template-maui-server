namespace Template.MobileServer.Web.Application.Context;

public sealed class ServiceContextEndpointFilter : IEndpointFilter
{
    private ApplicationServiceContextProvider Provider { get; }

    public ServiceContextEndpointFilter(ApplicationServiceContextProvider provider)
    {
        Provider = provider;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        using var scope = Provider.Begin(() => HttpServiceContext.GetOrCreate(context.HttpContext));
        return await next(context);
    }
}
