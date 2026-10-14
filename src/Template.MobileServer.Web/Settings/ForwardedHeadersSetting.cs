namespace Template.MobileServer.Web.Settings;

public sealed class ForwardedHeadersSetting
{
    public IReadOnlyList<string> KnownNetworks { get; set; } = [];

    public IReadOnlyList<string> KnownProxies { get; set; } = [];

    [Range(1, 10)]
    public int ForwardLimit { get; set; } = 1;
}
