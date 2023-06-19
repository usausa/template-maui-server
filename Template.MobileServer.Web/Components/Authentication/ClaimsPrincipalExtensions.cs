namespace Template.MobileServer.Web.Components.Authentication;

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static async ValueTask<Account> ToAccount(this Task<AuthenticationState> state)
    {
        var principal = (await state).User;
        return ToAccount(principal);
    }

    public static Account ToAccount(this ClaimsPrincipal principal)
    {
        if (principal.Identity is null)
        {
            return Account.Empty;
        }

        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value;
        var group = principal.FindFirst(Claims.Group)?.Value;

        if ((id is null) || (name is null) || (group is null))
        {
            return Account.Empty;
        }

        return new Account(id, name, group);
    }
}
