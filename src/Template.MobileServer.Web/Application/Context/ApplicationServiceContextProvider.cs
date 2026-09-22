namespace Template.MobileServer.Web.Application.Context;

// AsyncLocal で非同期フローに沿って運ぶ。Begin は境界だけが呼ぶ
public sealed class ApplicationServiceContextProvider : ServiceContextProvider
{
    private readonly AsyncLocal<Lazy<ServiceContext>?> local = new();

    // 未開始は例外にする (システムユーザーへの暗黙のフォールバックはしない)
    public override ServiceContext Current =>
        (local.Value ?? throw new InvalidOperationException("Service context scope is not started.")).Value;

    // 値は最初に読まれたときに 1 回だけ作る (読まない操作では作らない。同じスコープ内は同じ値)
    public IDisposable Begin(Func<ServiceContext> factory)
    {
        var scope = new Scope(local, local.Value);
        local.Value = new Lazy<ServiceContext>(factory);
        return scope;
    }

    // 入れ子は外側を退避し、Dispose で戻す
    private sealed class Scope(AsyncLocal<Lazy<ServiceContext>?> local, Lazy<ServiceContext>? previous) : IDisposable
    {
        public void Dispose() => local.Value = previous;
    }
}
