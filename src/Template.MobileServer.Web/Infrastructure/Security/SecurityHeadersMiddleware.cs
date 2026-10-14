namespace Template.MobileServer.Web.Infrastructure.Security;

public sealed class SecurityHeadersMiddleware
{
    private const string NoncePlaceholder = "{nonce}";

    private RequestDelegate Next { get; }

    private SecurityHeadersOption Option { get; }

    private readonly bool useNonce;

    private readonly Func<object, Task> onStarting;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOption option)
    {
        Next = next;
        Option = option;
        useNonce = option.ContentSecurityPolicy?.Contains(NoncePlaceholder, StringComparison.Ordinal) ?? false;
        onStarting = OnStarting;
    }

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(onStarting, context);
        return Next(context);
    }

    private Task OnStarting(object state)
    {
        var context = (HttpContext)state;
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        if (Option.ContentSecurityPolicy is not null)
        {
            var policy = useNonce
                ? Option.ContentSecurityPolicy.Replace(NoncePlaceholder, context.RequestServices.GetRequiredService<CspNonce>().Value, StringComparison.Ordinal)
                : Option.ContentSecurityPolicy;
            if (Option.ReportOnly)
            {
                headers.ContentSecurityPolicyReportOnly = policy;
            }
            else
            {
                headers.ContentSecurityPolicy = policy;
            }
        }

        return Task.CompletedTask;
    }
}
