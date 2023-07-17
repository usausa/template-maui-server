namespace Template.MobileServer.Backend.Web.Controllers;

using Smart.AspNetCore.Filters;

using Template.MobileServer.Backend.Components.Storage;

public class StorageController : BaseApiController
{
    private const string ContextType = "application/octet-stream";

    private ILogger<StorageController> Log { get; }

    private IStorage Storage { get; }

    public StorageController(
        ILogger<StorageController> log,
        IStorage storage)
    {
        Log = log;
        Storage = storage;
    }

    [HttpGet("{**path}")]
    public async ValueTask<IActionResult> Get([FromRoute] string? path = "/")
    {
        Log.InfoGet(path);

        if (path!.EndsWith('/'))
        {
            if (!await Storage.DirectoryExistsAsync(path).ConfigureAwait(false))
            {
                Log.WarnNotFound(path);

                return NotFound();
            }

            var files = await Storage.ListAsync(path).ConfigureAwait(false);
            return Ok(files);
        }

        if (!await Storage.FileExistsAsync(path).ConfigureAwait(false))
        {
            Log.WarnNotFound(path);

            return NotFound();
        }

        var stream = await Storage.ReadAsync(path).ConfigureAwait(false);
        var index = path.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
        return File(stream, ContextType, index >= 0 ? path[(index + 1)..] : path);
    }

    [HttpPost("{**path}")]
    [ReadableBodyStream]
    public async ValueTask<IActionResult> Post([FromRoute] string path)
    {
        Log.InfoPost(path);

        await Storage.WriteAsync(path, Request.Body).ConfigureAwait(false);

        return Ok();
    }

    [HttpDelete("{**path}")]
    public async ValueTask<IActionResult> Delete([FromRoute] string path)
    {
        Log.InfoDelete(path);

        await Storage.DeleteAsync(path).ConfigureAwait(false);

        return Ok();
    }
}
