namespace Template.MobileServer.Backend.Web.Infrastructure;

public static class Default<T>
    where T : new()
{
    public static T Instance => new();
}
