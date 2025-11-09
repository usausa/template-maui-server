namespace Template.MobileServer.Web.Api.Rest.Controllers;

using Template.MobileServer.Web.Api.Rest.Models;

public sealed class ServerController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new ServerTimeResponse { DateTime = DateTime.Now });
    }
}
