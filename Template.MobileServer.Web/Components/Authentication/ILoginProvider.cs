namespace Template.MobileServer.Web.Components.Authentication;

using System.Security.Claims;

public interface ILoginProvider
{
    Task LoginAsync(ClaimsIdentity identity);

    Task LogoutAsync();

    Task UpdateToken();
}
