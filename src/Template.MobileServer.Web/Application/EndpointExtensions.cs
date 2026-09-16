namespace Template.MobileServer.Web.Application;

using Template.MobileServer.Web.Infrastructure.Filters;

public static class EndpointExtensions
{
    public static RouteGroupBuilder MapApiGroup(this IEndpointRouteBuilder endpoints, string prefix) =>
        endpoints.MapGroup(prefix).AddEndpointFilter<RequestMetricsEndpointFilter>();
}
