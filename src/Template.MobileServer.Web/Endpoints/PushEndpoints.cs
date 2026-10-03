namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Services;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class PushSendRequest
{
    // 省略で全端末
    [RegularExpression(DeviceIdFormat.Pattern)]
    public string? DeviceId { get; set; }

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

        group.MapPost("/", HandleSendAsync);
    }

    //--------------------------------------------------------------------------------
    // Send
    //--------------------------------------------------------------------------------

    // 宛先の端末 (省略で全端末) の登録済みで有効な端末へ送り、送った件数を返す。宛先の端末が無い (未登録か無効) なら 404
    private static async ValueTask<IResult> HandleSendAsync(
        PushNotifier notifier,
        PushSendRequest request,
        CancellationToken cancellationToken)
    {
        var messages = await notifier.SendAsync(request.DeviceId, request.Title.Trim(), request.Body?.Trim() ?? string.Empty, cancellationToken);
        return (request.DeviceId is not null) && (messages.Count == 0)
            ? TypedResults.NotFound()
            : TypedResults.Ok(new PushSendResponse { Count = messages.Count });
    }
}
