namespace Template.MobileServer.Web.Application.Context;

public sealed class ServiceContextEndpointFilter : IEndpointFilter
{
    private readonly ApplicationServiceContextProvider provider;

    public ServiceContextEndpointFilter(ApplicationServiceContextProvider provider)
    {
        this.provider = provider;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        using var scope = provider.Begin(() => HttpServiceContext.GetOrCreate(context.HttpContext));
        return await next(context);
    }
}
