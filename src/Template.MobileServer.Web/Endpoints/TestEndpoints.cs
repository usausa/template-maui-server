namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class TestEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapTestEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Test);

        group.MapGet("/error/{code:int}", HandleError)
            .WithName("TestError")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet("/delay/{timeout:int}", HandleDelayAsync)
            .WithName("TestDelay")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    //--------------------------------------------------------------------------------
    // Error
    //--------------------------------------------------------------------------------

    private static IResult HandleError(int code) =>
        code switch
        {
            400 => TypedResults.BadRequest(),
            403 => TypedResults.Forbid(),
            404 => TypedResults.NotFound(),
            _ => throw new InvalidOperationException("Test exception.")
        };

    //--------------------------------------------------------------------------------
    // Delay
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleDelayAsync(
        [Range(0, 60_000)] int timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(timeout), timeProvider, cancellationToken);

        return TypedResults.Ok();
    }
}
