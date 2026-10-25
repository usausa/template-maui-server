namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Services;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class PushSendRequest
{
    [RegularExpression(TerminalIdFormat.Pattern)]
    public string? TerminalId { get; set; }

    [Required]
    [MaxLength(Length.Title)]
    public string Title { get; set; } = default!;

    [MaxLength(Length.Body)]
    public string? Body { get; set; }
}

public sealed class PushSendResponse
{
    public int Count { get; set; }
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class PushEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapPushEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Push);

        group.MapPost("/", HandleSendAsync)
            .WithName("PushSend")
            .Produces<PushSendResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    //--------------------------------------------------------------------------------
    // Send
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleSendAsync(
        PushNotifier notifier,
        PushSendRequest request)
    {
        var messages = await notifier.SendAsync(request.TerminalId, request.Title.Trim(), request.Body?.Trim() ?? string.Empty);
        return (request.TerminalId is not null) && (messages.Count == 0)
            ? TypedResults.NotFound()
            : TypedResults.Ok(new PushSendResponse { Count = messages.Count });
    }
}
