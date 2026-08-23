namespace Template.MobileServer.Web.Infrastructure.Filters;

using Template.MobileServer.Infrastructure.Storage;

public sealed class StorageExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (StorageException)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid path.");
        }
    }
}
