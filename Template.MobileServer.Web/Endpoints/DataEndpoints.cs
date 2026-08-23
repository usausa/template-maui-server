namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Models.Api;
using Template.MobileServer.Web.Models.Data;

public static class DataEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Data);

        // モバイルクライアント向け(HttpService の契約)
        group.MapGet("/list", HandleListAsync);

        // CRUD (要認証)
        group.MapGet("/{id:long}", HandleGetAsync).RequireAuthorization();
        group.MapPost("/", HandleCreateAsync).RequireAuthorization();
        group.MapPut("/{id:long}", HandleUpdateAsync).RequireAuthorization();
        group.MapDelete("/{id:long}", HandleDeleteAsync).RequireAuthorization();
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(DataService dataService)
    {
        var entities = await dataService.QueryListAsync();
        return TypedResults.Ok(new DataListResponse
        {
            Entries = entities.Select(static x => new DataListResponseEntry { Id = x.Id, Name = x.Name }).ToList()
        });
    }

    private static async ValueTask<IResult> HandleGetAsync(
        DataService dataService,
        long id)
    {
        var entity = await dataService.QueryAsync(id);
        return entity is not null
            ? TypedResults.Ok(MapToResponse(entity))
            : TypedResults.NotFound();
    }

    private static async ValueTask<IResult> HandleCreateAsync(
        DataService dataService,
        DataCreateRequest request)
    {
        var id = await dataService.InsertAsync(request.Name, request.Value);
        return id.HasValue
            ? TypedResults.Created($"{ApiRoutes.Data}/{id.Value}", new DataCreateResponse(id.Value))
            : TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate name.");
    }

    private static async ValueTask<IResult> HandleUpdateAsync(
        DataService dataService,
        long id,
        DataUpdateRequest request)
    {
        var result = await dataService.UpdateAsync(id, request.Name, request.Value);
        return result switch
        {
            DataWriteStatus.Success => TypedResults.NoContent(),
            DataWriteStatus.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate name.")
        };
    }

    private static async ValueTask<IResult> HandleDeleteAsync(
        DataService dataService,
        long id)
    {
        var deleted = await dataService.DeleteAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    //--------------------------------------------------------------------------------
    // Mapper
    //--------------------------------------------------------------------------------

    private static DataResponse MapToResponse(DataEntity entity) =>
        new(entity.Id, entity.Name, entity.Value, entity.CreatedAt);
}
