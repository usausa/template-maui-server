namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Models.Api;

public static class ServerEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapServerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Server);

        group.MapGet("/time", HandleTime);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static Ok<ServerTimeResponse> HandleTime(TimeProvider timeProvider) =>
        TypedResults.Ok(new ServerTimeResponse { DateTime = timeProvider.GetLocalNow().DateTime });
}
