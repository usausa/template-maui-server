namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Models.Api;

public static class SecretEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapSecretEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Secret)
            .RequireAuthorization(Policies.MobileApi);

        group.MapGet("/message", HandleMessage);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static Ok<SecretMessageResponse> HandleMessage(ClaimsPrincipal user) =>
        TypedResults.Ok(new SecretMessageResponse { Message = $"Hello {user.Identity?.Name}" });
}
