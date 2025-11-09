namespace Template.MobileServer.Web.Api.Rest;

using Template.MobileServer.Web.Application.Filters;

[Area("api")]
[Microsoft.AspNetCore.Mvc.Route("[area]/[controller]/[action]")]
[ApiController]
[HttpExceptionFilter]
public abstract class BaseApiController : ControllerBase
{
}
