namespace Template.MobileServer.Backend.Web.Controllers;

using Template.MobileServer.Backend.Web;

public class TestController : BaseApiController
{
    [HttpGet]
    public IActionResult Error(int code)
    {
        return code switch
        {
            404 => NotFound(),
            403 => Forbid(),
            _ => throw new NotSupportedException("Unknown error.")
        };
    }
}
