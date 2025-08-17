namespace Template.MobileServer.Backend.Web.Api.Controllers;

#pragma warning disable ASP0023
public class TestController : BaseApiController
{
    [HttpGet("{code:int}")]
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

    [HttpGet("{timeout:int}")]
    public async ValueTask<IActionResult> Delay(int timeout)
    {
        await Task.Delay(timeout);

        return Ok();
    }
}
#pragma warning restore ASP0023
