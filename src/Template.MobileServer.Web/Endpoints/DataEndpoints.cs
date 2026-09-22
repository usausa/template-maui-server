namespace Template.MobileServer.Web.Endpoints;

using Smart.Mapper;

using Template.MobileServer.Web.Application;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class DataListEntry
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;
}

public sealed class DataListResponse
{
    public IReadOnlyList<DataListEntry> Entries { get; set; } = default!;

    public int Total { get; set; }
}

public sealed class DataResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class DataCreateRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = default!;

    [Range(0, 1_000_000)]
    public int Value { get; set; }
}

public sealed class DataCreateResponse
{
    public long Id { get; set; }
}

public sealed class DataUpdateRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = default!;

    [Range(0, 1_000_000)]
    public int Value { get; set; }
}

//--------------------------------------------------------------------------------
// Mapper
//--------------------------------------------------------------------------------

public static partial class DataMapper
{
    [Mapper]
    public static partial DataListEntry ToListEntry(this DataEntity entity);

    [Mapper]
    public static partial DataResponse ToResponse(this DataEntity entity);
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class DataEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapDataEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Data);

        group.MapGet("/list", HandleListAsync);
        group.MapGet("/{id:long}", HandleGetAsync);
        group.MapPost("/", HandleCreateAsync);
        group.MapPut("/{id:long}", HandleUpdateAsync);
        group.MapDelete("/{id:long}", HandleDeleteAsync);
    }

    //--------------------------------------------------------------------------------
    // List
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(
        DataService dataService,
        [Range(0, Int32.MaxValue)] int? offset,
        [Range(1, 200)] int? size,
        CancellationToken cancellationToken)
    {
        if (size is null)
        {
            var entities = await dataService.QueryAllAsync(cancellationToken);
            return TypedResults.Ok(new DataListResponse
            {
                Entries = entities.Select(static x => x.ToListEntry()).ToList(),
                Total = entities.Count
            });
        }

        var result = await dataService.QueryRangeAsync(offset ?? 0, size.Value, cancellationToken);
        return TypedResults.Ok(new DataListResponse
        {
            Entries = result.Items.Select(static x => x.ToListEntry()).ToList(),
            Total = result.Total
        });
    }

    //--------------------------------------------------------------------------------
    // Get
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleGetAsync(
        DataService dataService,
        long id)
    {
        var entity = await dataService.QueryAsync(id);
        return entity is not null
            ? TypedResults.Ok(entity.ToResponse())
            : TypedResults.NotFound();
    }

    //--------------------------------------------------------------------------------
    // Create
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleCreateAsync(
        DataService dataService,
        DataCreateRequest request)
    {
        var entity = new DataEntity { Name = request.Name, Value = request.Value };
        return await dataService.InsertAsync(entity) == DataWriteStatus.Success
            ? TypedResults.Created($"{ApiRoutes.Data}/{entity.Id}", new DataCreateResponse { Id = entity.Id })
            : TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate name");
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

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
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Duplicate name")
        };
    }

    //--------------------------------------------------------------------------------
    // Delete
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleDeleteAsync(
        DataService dataService,
        long id)
    {
        var result = await dataService.DeleteAsync(id);
        return result == DataWriteStatus.Success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
