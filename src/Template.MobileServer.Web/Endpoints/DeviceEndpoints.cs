namespace Template.MobileServer.Web.Endpoints;

using Smart.Mapper;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Telemetry;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class DeviceRegisterRequest
{
    [Required]
    [MaxLength(Length.Name)]
    public string Name { get; set; } = default!;
}

public sealed class DeviceRegisterResponse
{
    public string DeviceId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? GroupName { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime RegisteredAt { get; set; }
}

//--------------------------------------------------------------------------------
// Mapper
//--------------------------------------------------------------------------------

public static partial class DeviceMapper
{
    [Mapper]
    public static partial DeviceRegisterResponse ToRegisterResponse(this DeviceEntity entity);
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class DeviceEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Device);

        group.MapPut("/{deviceId}", HandleRegisterAsync);
    }

    //--------------------------------------------------------------------------------
    // Register
    //--------------------------------------------------------------------------------

    // 端末からの登録。未登録なら登録して 201、登録済みなら名前を更新して 200 (グループ・メモ・有効は管理画面で変える)
    private static async ValueTask<IResult> HandleRegisterAsync(
        TelemetryDeviceRegistry registry,
        [RegularExpression(DeviceIdFormat.Pattern)] string deviceId,
        DeviceRegisterRequest request,
        CancellationToken cancellationToken)
    {
        var (device, created) = await registry.RegisterAsync(deviceId, request.Name.Trim(), cancellationToken);
        var response = device.ToRegisterResponse();
        return created
            ? TypedResults.Created($"{ApiRoutes.Device}/{deviceId}", response)
            : TypedResults.Ok(response);
    }
}
