namespace Template.MobileServer.Backend.Web.Controllers;

using Template.MobileServer.Backend.Web;

public class TestController : BaseApiController
{
    [HttpPost("{code}")]
    public IActionResult Error(int code)
    {
        return code switch
        {
            400 => BadRequest(),
            404 => NotFound(),
            403 => Forbid(),
            _ => throw new NotSupportedException("Unknown error.")
        };
    }

    [HttpPost("{timeout}")]
    public async ValueTask<IActionResult> Delay(int timeout)
    {
        await Task.Delay(timeout);

        return Ok();
    }
}
