namespace Template.MobileServer.Application.Context;

using Template.MobileServer.Services;
using Template.MobileServer.Web.Application.Context;

public sealed class ApplicationServiceContextProviderTests
{
    private static readonly ServiceContext Outer = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero), "outer");

    private static readonly ServiceContext Inner = new(new DateTimeOffset(2026, 9, 22, 12, 0, 1, TimeSpan.Zero), "inner");

    // 未開始は例外
    [Fact]
    public void CurrentThrowsWhenNotStarted()
    {
        var provider = new ApplicationServiceContextProvider();

        Assert.Throws<InvalidOperationException>(() => provider.Current);
    }

    // スコープ内は同じ値、入れ子は外側へ戻る、await をまたいでも保持される
    [Fact]
    public async Task ScopeFlowsAndRestores()
    {
        var provider = new ApplicationServiceContextProvider();

        using (provider.Begin(static () => Outer))
        {
            Assert.Same(Outer, provider.Current);

            using (provider.Begin(static () => Inner))
            {
                await Task.Yield();
                Assert.Same(Inner, provider.Current);
            }

            Assert.Same(Outer, provider.Current);
        }

        Assert.Throws<InvalidOperationException>(() => provider.Current);
    }

    // 値は最初に読まれたときに 1 回だけ作られる (読まなければ作られない)
    [Fact]
    public void ContextIsCreatedLazilyAndOnce()
    {
        var provider = new ApplicationServiceContextProvider();
        var created = 0;

        using (provider.Begin(() =>
        {
            created++;
            return Outer;
        }))
        {
            Assert.Equal(0, created);
            Assert.Same(provider.Current, provider.Current);
            Assert.Equal(1, created);
        }

        Assert.Equal(1, created);
    }
}
