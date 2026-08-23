namespace Template.MobileServer.Web.Infrastructure.Logging;

public static class LoggingContext
{
    private static readonly AsyncLocal<string?> UserIdLocal = new();

    public static string? UserId
    {
        get => UserIdLocal.Value;
        set => UserIdLocal.Value = value;
    }
}
