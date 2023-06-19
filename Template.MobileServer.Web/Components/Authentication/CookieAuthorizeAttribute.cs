namespace Template.MobileServer.Web.Components.Authentication;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CookieAuthorizeAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new CookieAuthorizeFilter(serviceProvider.GetRequiredService<IOptions<CookieAuthenticationSetting>>().Value);
    }

    public sealed class CookieAuthorizeFilter : IAuthorizationFilter
    {
        private readonly CookieAuthenticationSetting setting;

        private readonly byte[] secretKey;

        public CookieAuthorizeFilter(CookieAuthenticationSetting setting)
        {
            this.setting = setting;
            secretKey = HexEncoder.Decode(setting.SecretKey);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var value = context.HttpContext.Request.Cookies[setting.AccountKey];
            if (!String.IsNullOrEmpty(value))
            {
                var principal = TokenHelper.ParseToken(value, secretKey, setting.Issuer);
                if (principal is not null)
                {
                    context.HttpContext.User = principal;
                    return;
                }
            }

            context.Result = new UnauthorizedResult();
        }
    }
}
