namespace Template.MobileServer.Application.Context;

using Template.MobileServer.Services;
using Template.MobileServer.Web.Application.Context;

public sealed class ApplicationServiceContextProviderTests
{
    private static readonly ServiceContext Outer = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero), "outer");

    private static readonly ServiceContext Inner = new(new DateTimeOffset(2026, 9, 22, 12, 0, 1, TimeSpan.Zero), "inner");

    [Fact]
    public void CurrentThrowsWhenNotStarted()
    {
        var provider = new ApplicationServiceContextProvider();

        Assert.Throws<InvalidOperationException>(() => provider.Current);
    }

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
