namespace Template.MobileServer.Web.Api;

using Template.MobileServer.Web.Infrastructure.Filters;

[Area("api")]
[Microsoft.AspNetCore.Mvc.Route("[area]/[controller]/[action]")]
[ApiController]
[ApiExceptionFilter]
public class BaseApiController : ControllerBase
{
}
