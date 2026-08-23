namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Infrastructure.Authentication;
using Template.MobileServer.Web.Models.Api;

public static class AccountEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Account);

        group.MapPost("/login", HandleLogin);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static Ok<AccountLoginResponse> HandleLogin(
        AccountLoginRequest request,
        TokenService tokenService) =>
        TypedResults.Ok(new AccountLoginResponse { Token = tokenService.CreateToken(request.Id) });
}
