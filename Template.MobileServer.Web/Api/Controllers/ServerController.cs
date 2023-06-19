namespace Template.MobileServer.Web.Api.Controllers;

using Template.MobileServer.Web.Api.Models;

public class ServerController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new ServerTimeResponse { DateTime = DateTime.Now });
    }
}
