namespace Template.MobileServer.Web.Api.Http;

using Template.MobileServer.Web.Infrastructure.Filters;

[Area("api")]
[Microsoft.AspNetCore.Mvc.Route("[area]/[controller]/[action]")]
[ApiController]
[HttpExceptionFilter]
public abstract class BaseApiController : ControllerBase
{
}
