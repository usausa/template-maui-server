namespace Template.MobileServer.Web.Components.Authentication;

using System.Security.Claims;

public sealed class LoginManager
{
    private readonly ILoginProvider loginProvider;

    public LoginManager(ILoginProvider loginProvider)
    {
        this.loginProvider = loginProvider;
    }

    public async Task<bool> LoginAsync(string id, string password)
    {
        // TODO custom
        if (id != password)
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, id),
            new(Claims.Group, "00")
        };
        if (id == "admin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        }

        var identify = new ClaimsIdentity(claims, "custom");
        await loginProvider.LoginAsync(identify);

        return true;
    }

    public Task LogoutAsync() => loginProvider.LogoutAsync();

    public Task UpdateToken() => loginProvider.UpdateToken();
}
