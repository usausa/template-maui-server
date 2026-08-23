namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Models.Api;

public static class TestEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapTestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Test);

        group.MapGet("/time", HandleTime);
        group.MapGet("/error/{code:int}", HandleError);
        group.MapGet("/delay/{timeout:int}", HandleDelayAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static Ok<TestTimeResponse> HandleTime(TimeProvider timeProvider) =>
        TypedResults.Ok(new TestTimeResponse { DateTime = timeProvider.GetLocalNow().DateTime });

    private static IResult HandleError(int code) =>
        code switch
        {
            400 => TypedResults.BadRequest(),
            403 => TypedResults.Forbid(),
            404 => TypedResults.NotFound(),
            _ => throw new InvalidOperationException("Test exception.")
        };

    private static async ValueTask<IResult> HandleDelayAsync(
        [Range(0, 60_000)] int timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(timeout), timeProvider, cancellationToken);

        return TypedResults.Ok();
    }
}
