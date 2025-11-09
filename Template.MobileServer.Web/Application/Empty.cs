namespace Template.MobileServer.Web.Application;

public static class Empty<T>
    where T : new()
{
    public static T Instance { get; } = new();
}
