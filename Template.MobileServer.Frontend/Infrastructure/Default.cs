namespace Template.MobileServer.Frontend.Infrastructure;

public static class Default<T>
    where T : new()
{
    public static T Instance => new();
}
