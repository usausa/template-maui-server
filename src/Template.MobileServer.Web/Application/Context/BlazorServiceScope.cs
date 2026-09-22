namespace Template.MobileServer.Web.Application.Context;

// Blazor の境界 (回線単位)。管理画面は認証なしなので実行ユーザーは固定
public sealed class BlazorServiceScope
{
    private const string GuestUserId = "guest";

    private readonly TimeProvider timeProvider;

    private readonly ApplicationServiceContextProvider provider;

    // イベントごとのデリゲート生成を避けるため 1 回だけ作る
    private readonly Func<ServiceContext> factory;

    public BlazorServiceScope(
        TimeProvider timeProvider,
        ApplicationServiceContextProvider provider)
    {
        this.timeProvider = timeProvider;
        this.provider = provider;
        factory = CreateContext;
    }

    public IDisposable Begin() => provider.Begin(factory);

    private ServiceContext CreateContext() => new(timeProvider.GetLocalNow(), GuestUserId);
}
