namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class SecretMessageResponse
{
    public string Message { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class SecretEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapSecretEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Secret)
            .RequireAuthorization(Policies.MobileApi)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/message", HandleMessage)
            .WithName("SecretMessage");
    }

    //--------------------------------------------------------------------------------
    // Message
    //--------------------------------------------------------------------------------

    private static Ok<SecretMessageResponse> HandleMessage(ClaimsPrincipal user) =>
        TypedResults.Ok(new SecretMessageResponse { Message = $"Hello {user.Identity?.Name}" });
}
