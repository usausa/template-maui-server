namespace Template.MobileServer.Web.Application.Context;

public sealed class ApplicationServiceContextProvider : ServiceContextProvider
{
    private readonly AsyncLocal<Lazy<ServiceContext>?> local = new();

    public override ServiceContext Current =>
        (local.Value ?? throw new InvalidOperationException("Service context scope is not started.")).Value;

    public IDisposable Begin(Func<ServiceContext> factory)
    {
        var scope = new Scope(local, local.Value);
        local.Value = new Lazy<ServiceContext>(factory);
        return scope;
    }

    private sealed class Scope(AsyncLocal<Lazy<ServiceContext>?> local, Lazy<ServiceContext>? previous) : IDisposable
    {
        public void Dispose() => local.Value = previous;
    }
}
