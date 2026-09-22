namespace Template.MobileServer.Web.Infrastructure.Security;

public sealed class SecurityHeadersOption
{
    // Content-Security-Policy is only reported, not enforced
    public bool ReportOnly { get; set; }

    // Content-Security-Policy value. "{nonce}" is replaced with the CspNonce of the request. null = no CSP header
    public string? ContentSecurityPolicy { get; set; }
}
