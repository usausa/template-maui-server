namespace Template.MobileServer.Web.Components.Authentication;

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

public sealed class CookieAuthenticationStateProvider : AuthenticationStateProvider, ILoginProvider
{
    private static readonly DateTime ExpireDate = new(1970, 1, 1);

    private static readonly ClaimsPrincipal Anonymous = new();

    private readonly IHttpContextAccessor httpContextAccessor;

    private readonly IJSRuntime jsRuntime;

    private readonly CookieAuthenticationSetting setting;

    private readonly byte[] secretKey;

    private ClaimsPrincipal? cachedPrincipal;

    public CookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime, IOptions<CookieAuthenticationSetting> setting)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.jsRuntime = jsRuntime;
        this.setting = setting.Value;
        secretKey = HexEncoder.Decode(setting.Value.SecretKey);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (cachedPrincipal is not null)
        {
            return Task.FromResult(new AuthenticationState(cachedPrincipal));
        }

        var principal = LoadAccount();
        if (principal is not null)
        {
            cachedPrincipal = principal;
            return Task.FromResult(new AuthenticationState(principal));
        }

        return Task.FromResult(new AuthenticationState(Anonymous));
    }

    public async Task LoginAsync(ClaimsIdentity identity)
    {
        var value = TokenHelper.BuildToken(identity, secretKey, setting.Issuer, setting.Expire);
        await UpdateCookie(value, DateTime.Now.AddMinutes(setting.Expire)).ConfigureAwait(false);

        cachedPrincipal = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(cachedPrincipal)));
    }

    public async Task LogoutAsync()
    {
        cachedPrincipal = null;
        await UpdateCookie(string.Empty, ExpireDate).ConfigureAwait(false);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private ClaimsPrincipal? LoadAccount()
    {
        var value = httpContextAccessor.HttpContext?.Request.Cookies[setting.AccountKey];
        if (String.IsNullOrEmpty(value))
        {
            return null;
        }

        return TokenHelper.ParseToken(value, secretKey, setting.Issuer);
    }

    public async Task UpdateToken()
    {
        if (cachedPrincipal is null)
        {
            return;
        }

        var identity = cachedPrincipal.Identities.First();
        var value = TokenHelper.BuildToken(identity, secretKey, setting.Issuer, setting.Expire);
        await UpdateCookie(value, DateTime.Now.AddMinutes(setting.Expire)).ConfigureAwait(false);
    }

    private async Task UpdateCookie(string value, DateTime expire)
    {
        await jsRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{setting.AccountKey}={value}; path=/; expires={expire.ToUniversalTime():R}\"").ConfigureAwait(false);
    }
}
