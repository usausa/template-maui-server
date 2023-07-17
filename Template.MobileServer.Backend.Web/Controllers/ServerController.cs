namespace Template.MobileServer.Backend.Web.Controllers;

using Template.MobileServer.Backend.Web;

public class ServerController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new ServerTimeResponse { DateTime = DateTime.Now });
    }
}
