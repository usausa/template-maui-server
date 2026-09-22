namespace Template.MobileServer.Web.Application.Context;

public static class HttpServiceContext
{
    private const string ServiceContextKey = "__ServiceContext";

    private const string AnonymousUserId = "anonymous";

    public static ServiceContext GetOrCreate(HttpContext httpContext)
    {
        if (httpContext.Items[ServiceContextKey] is ServiceContext existing)
        {
            return existing;
        }

        var context = Create(httpContext);
        httpContext.Items[ServiceContextKey] = context;

        return context;
    }

    public static ServiceContext Create(HttpContext httpContext) =>
        new(httpContext.RequestServices.GetRequiredService<TimeProvider>().GetLocalNow(), GetUserId(httpContext.User));

    public static string GetUserId(ClaimsPrincipal? user)
    {
        var identity = user?.Identity;
        return (identity?.IsAuthenticated ?? false) && !String.IsNullOrEmpty(identity.Name) ? identity.Name : AnonymousUserId;
    }
}
