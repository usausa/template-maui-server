namespace Template.MobileServer.Web.Components.Authentication;

public sealed class Account
{
    public static Account Empty => new(string.Empty, string.Empty, string.Empty);

    public string Id { get; }

    public string Name { get; }

    public string Group { get; }

    public Account(string id, string name, string group)
    {
        Id = id;
        Name = name;
        Group = group;
    }
}
