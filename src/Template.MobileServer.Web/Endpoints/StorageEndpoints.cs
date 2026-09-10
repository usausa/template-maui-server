namespace Template.MobileServer.Web.Endpoints;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Infrastructure.Filters;
using Template.MobileServer.Web.Models.Api;

public static class StorageEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapStorageEndpoints(this WebApplication app)
    {
        // [MEMO] モバイル契約維持のため匿名のまま(JWT保護化は拡張候補)
        var group = app.MapGroup(ApiRoutes.Storage)
            .AddEndpointFilter<StorageExceptionFilter>();

        // 簡易FTP契約: 末尾スラッシュまたは空パス=ディレクトリ一覧、それ以外=ファイルダウンロード
        group.MapGet("/{**path}", HandleGetAsync);
        group.MapPost("/{**path}", HandleUploadAsync);
        group.MapDelete("/{**path}", HandleDeleteAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
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
                Entries = entries.Select(static x => new StorageListResponseEntry
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

    private static async ValueTask<IResult> HandleUploadAsync(
        HttpContext context,
        IStorage storage,
        string path)
    {
        // クライアント(HttpService.UploadAsync)は本文に生ストリームを送る(親ディレクトリは自動作成)
        await storage.WriteAsync(path, context.Request.Body, context.RequestAborted);

        return TypedResults.Ok();
    }

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

        // ディレクトリは再帰削除
        if (await storage.DirectoryExistsAsync(path, cancellationToken))
        {
            await storage.DeleteAsync(path, cancellationToken);
            return TypedResults.NoContent();
        }

        return TypedResults.NotFound();
    }
}
