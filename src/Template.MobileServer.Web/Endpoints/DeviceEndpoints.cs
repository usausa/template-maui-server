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
    public string TerminalId { get; set; } = default!;

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

        group.MapPut("/{terminalId}", HandleRegisterAsync)
            .WithName("DeviceRegister")
            .Produces<DeviceRegisterResponse>()
            .Produces<DeviceRegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }

    //--------------------------------------------------------------------------------
    // Register
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleRegisterAsync(
        TelemetryDeviceRegistry registry,
        [RegularExpression(TerminalIdFormat.Pattern)] string terminalId,
        DeviceRegisterRequest request)
    {
        var (device, created) = await registry.RegisterAsync(terminalId, request.Name.Trim());
        var response = device.ToRegisterResponse();
        return created
            ? TypedResults.Created($"{ApiRoutes.Device}/{terminalId}", response)
            : TypedResults.Ok(response);
    }
}
