namespace Template.MobileServer.Web.Infrastructure.Security;

public sealed class SecurityHeadersMiddleware
{
    private const string NoncePlaceholder = "{nonce}";

    private readonly RequestDelegate next;

    private readonly SecurityHeadersOption option;

    private readonly bool useNonce;

    private readonly Func<object, Task> onStarting;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOption option)
    {
        this.next = next;
        this.option = option;
        useNonce = option.ContentSecurityPolicy?.Contains(NoncePlaceholder, StringComparison.Ordinal) ?? false;
        onStarting = OnStarting;
    }

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(onStarting, context);
        return next(context);
    }

    private Task OnStarting(object state)
    {
        var context = (HttpContext)state;
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        if (option.ContentSecurityPolicy is not null)
        {
            var policy = useNonce
                ? option.ContentSecurityPolicy.Replace(NoncePlaceholder, context.RequestServices.GetRequiredService<CspNonce>().Value, StringComparison.Ordinal)
                : option.ContentSecurityPolicy;
            if (option.ReportOnly)
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
