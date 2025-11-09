namespace Template.MobileServer.Web.Infrastructure;

public static class Empty<T>
    where T : new()
{
    public static T Instance { get; } = new();
}
