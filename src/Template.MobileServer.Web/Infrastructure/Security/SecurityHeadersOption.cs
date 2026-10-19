namespace Template.MobileServer.Web.Infrastructure.Security;

public sealed class SecurityHeadersOption
{
    public bool ReportOnly { get; set; }

    public string? ContentSecurityPolicy { get; set; }
}
