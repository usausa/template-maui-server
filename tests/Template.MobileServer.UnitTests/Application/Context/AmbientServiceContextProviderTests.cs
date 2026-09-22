namespace Template.MobileServer.Application.Context;

using Template.MobileServer.Models;
using Template.MobileServer.Web.Application.Context;

public sealed class AmbientServiceContextProviderTests
{
    private static readonly ServiceContext Outer = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero), "outer", "outer");

    private static readonly ServiceContext Inner = new(new DateTimeOffset(2026, 9, 22, 12, 0, 1, TimeSpan.Zero), "inner", "inner");

    // 未開始は例外
    [Fact]
    public void CurrentThrowsWhenNotStarted()
    {
        var provider = new AmbientServiceContextProvider();

        Assert.Throws<InvalidOperationException>(() => provider.Current);
    }

    // スコープ内は同じ値、入れ子は外側へ戻る、await をまたいでも保持される
    [Fact]
    public async Task ScopeFlowsAndRestores()
    {
        var provider = new AmbientServiceContextProvider();

        using (provider.Begin(Outer))
        {
            Assert.Same(Outer, provider.Current);

            using (provider.Begin(Inner))
            {
                await Task.Yield();
                Assert.Same(Inner, provider.Current);
            }

            Assert.Same(Outer, provider.Current);
        }

        Assert.Throws<InvalidOperationException>(() => provider.Current);
    }
}
