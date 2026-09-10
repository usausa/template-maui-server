namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Security;
using Template.MobileServer.Models.Entity;

public sealed class AccountService
{
    private readonly AccountAccessor accountAccessor;

    private readonly IPasswordProvider passwordProvider;

    private readonly TimeProvider timeProvider;

    public AccountService(
        AccountAccessor accountAccessor,
        IPasswordProvider passwordProvider,
        TimeProvider timeProvider)
    {
        this.accountAccessor = accountAccessor;
        this.passwordProvider = passwordProvider;
        this.timeProvider = timeProvider;
    }

    public async ValueTask InitializeAsync(string initialName, string initialPassword, string initialRole)
    {
        accountAccessor.Create();

        // Seed initial account
        var count = await accountAccessor.CountAsync();
        if (count == 0)
        {
            await accountAccessor.InsertAsync(initialName, passwordProvider.Generate(initialPassword), initialRole, timeProvider.GetLocalNow().DateTime);
        }
    }

    public async ValueTask<AccountEntity?> AuthenticateAsync(string name, string password)
    {
        var account = await accountAccessor.QueryByNameAsync(name);
        if (account is null)
        {
            return null;
        }

        return passwordProvider.Match(password, account.Password) ? account : null;
    }
}
