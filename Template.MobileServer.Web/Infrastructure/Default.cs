namespace Template.MobileServer.Web.Infrastructure;

public static class Default<T>
    where T : new()
{
    public static T Instance => new();
}
