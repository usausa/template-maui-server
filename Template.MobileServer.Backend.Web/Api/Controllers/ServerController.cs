namespace Template.MobileServer.Backend.Web.Api.Controllers;

using Template.MobileServer.Backend.Web.Api.Models;

public class ServerController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new ServerTimeResponse { DateTime = DateTime.Now });
    }
}
