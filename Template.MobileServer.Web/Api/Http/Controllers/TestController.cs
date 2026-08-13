namespace Template.MobileServer.Web.Api.Http.Controllers;

using Template.MobileServer.Web.Api.Http.Models;

public sealed class TestController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new TestTimeResponse { DateTime = DateTime.Now });
    }

    [HttpGet]
    public IActionResult Error()
    {
        throw new InvalidOperationException("API error.");
    }
}
