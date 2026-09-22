namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class ServerTimeResponse
{
    public DateTime DateTime { get; set; }
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class ServerEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapServerEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Server);

        group.MapGet("/time", HandleTime);
    }

    //--------------------------------------------------------------------------------
    // Time
    //--------------------------------------------------------------------------------

    private static Ok<ServerTimeResponse> HandleTime(TimeProvider timeProvider) =>
        TypedResults.Ok(new ServerTimeResponse { DateTime = timeProvider.GetLocalNow().DateTime });
}
