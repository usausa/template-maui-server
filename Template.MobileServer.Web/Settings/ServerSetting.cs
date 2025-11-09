namespace Template.MobileServer.Web.Settings;

public sealed class ServerSetting
{
    public int LongTimeThreshold { get; set; }

    public string[]? KnownProxies { get; set; }

    public string[]? KnownNetworks { get; set; }
}
