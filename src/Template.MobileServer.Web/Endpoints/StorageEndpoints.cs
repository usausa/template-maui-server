namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Application;

//--------------------------------------------------------------------------------
// Models
//--------------------------------------------------------------------------------

public sealed class StorageListEntry
{
    public string Name { get; set; } = default!;

    public bool Directory { get; set; }

    public long? Size { get; set; }

    public DateTime LastModified { get; set; }
}

public sealed class StorageListResponse
{
    public IReadOnlyList<StorageListEntry> Entries { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Endpoints
//--------------------------------------------------------------------------------

public static class StorageEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapStorageEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup(ApiRoutes.Storage)
            .AddEndpointFilter(static async (context, next) =>
            {
                try
                {
                    return await next(context);
                }
                catch (StorageException)
                {
                    return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid path.");
                }
            });

        group.MapGet("/{**path}", HandleGetAsync);
        group.MapPost("/{**path}", HandleUploadAsync);
        group.MapDelete("/{**path}", HandleDeleteAsync);
    }

    //--------------------------------------------------------------------------------
    // Get
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleGetAsync(
        IStorage storage,
        string? path,
        CancellationToken cancellationToken)
    {
        path ??= string.Empty;

        if ((path.Length == 0) || path.EndsWith('/'))
        {
            if (!await storage.DirectoryExistsAsync(path, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            var entries = await storage.ListEntriesAsync(path, cancellationToken);
            return TypedResults.Ok(new StorageListResponse
            {
                Entries = entries.Select(static x => new StorageListEntry
                {
                    Name = x.Name,
                    Directory = x.IsDirectory,
                    Size = x.IsDirectory ? null : x.Size,
                    LastModified = x.LastModified
                }).ToList()
            });
        }

        if (!await storage.FileExistsAsync(path, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var stream = await storage.ReadAsync(path, cancellationToken);
        return TypedResults.Stream(stream, "application/octet-stream", Path.GetFileName(path));
    }

    //--------------------------------------------------------------------------------
    // Upload
    //--------------------------------------------------------------------------------

    [DisableRequestSizeLimit]
    private static async ValueTask<IResult> HandleUploadAsync(
        HttpContext context,
        IStorage storage,
        string path)
    {
        await storage.WriteAsync(path, context.Request.Body, context.RequestAborted);

        return TypedResults.Ok();
    }

    //--------------------------------------------------------------------------------
    // Delete
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleDeleteAsync(
        IStorage storage,
        string path,
        CancellationToken cancellationToken)
    {
        if (await storage.FileExistsAsync(path, cancellationToken))
        {
            await storage.DeleteAsync(path, cancellationToken);
            return TypedResults.NoContent();
        }

        if (await storage.DirectoryExistsAsync(path, cancellationToken))
        {
            await storage.DeleteAsync(path, cancellationToken);
            return TypedResults.NoContent();
        }

        return TypedResults.NotFound();
    }
}
