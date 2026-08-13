namespace Template.MobileServer.Web.Api.Http.Controllers;

using Template.MobileServer.Web.Api.Http.Models;

public sealed class ServerController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new ServerTimeResponse { DateTime = DateTime.Now });
    }
}
