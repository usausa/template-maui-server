namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Application.Authentication;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class AccountLoginRequest
{
    [Required]
    public string Id { get; set; } = default!;
}

public sealed class AccountLoginResponse
{
    public string Token { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class AccountEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Account);

        group.MapPost("/login", HandleLogin);
    }

    //--------------------------------------------------------------------------------
    // Login
    //--------------------------------------------------------------------------------

    // [MEMO] Dummy login
    private static Ok<AccountLoginResponse> HandleLogin(
        AccountLoginRequest request,
        JwtTokenProvider tokenProvider) =>
        TypedResults.Ok(new AccountLoginResponse { Token = tokenProvider.CreateToken(request.Id) });
}
