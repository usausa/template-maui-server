namespace Template.MobileServer.Backend.Web;

using Template.MobileServer.Backend.Web.Infrastructure.Filters;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController]
[ApiExceptionFilter]
public class BaseApiController : ControllerBase
{
}
