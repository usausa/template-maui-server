namespace Template.MobileServer.Web.Application.Context;

public sealed class BlazorServiceScope
{
    private const string GuestUserId = "guest";

    private TimeProvider TimeProvider { get; }

    private ApplicationServiceContextProvider Provider { get; }

    private readonly Func<ServiceContext> factory;

    public BlazorServiceScope(
        TimeProvider timeProvider,
        ApplicationServiceContextProvider provider)
    {
        TimeProvider = timeProvider;
        Provider = provider;
        factory = CreateContext;
    }

    public IDisposable Begin() => Provider.Begin(factory);

    private ServiceContext CreateContext() => new(TimeProvider.GetLocalNow(), GuestUserId);
}
