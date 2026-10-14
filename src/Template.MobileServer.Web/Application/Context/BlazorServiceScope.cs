namespace Template.MobileServer.Web.Application.Context;

// Blazor の境界 (回線単位)。管理画面は認証なしなので実行ユーザーは固定
public sealed class BlazorServiceScope
{
    private const string GuestUserId = "guest";

    private TimeProvider TimeProvider { get; }

    private ApplicationServiceContextProvider Provider { get; }

    // イベントごとのデリゲート生成を避けるため 1 回だけ作る
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
