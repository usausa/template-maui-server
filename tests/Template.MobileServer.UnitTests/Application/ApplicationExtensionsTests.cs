namespace Template.MobileServer.Application;

using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MudBlazor;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Settings;

public sealed class ApplicationExtensionsTests
{
    // X-Forwarded-* を受けるのは、既定のループバックと設定のネットワーク・プロキシから。転送の段数は設定から、Server ヘッダーは出さない
    [Fact]
    public async Task ConfigureHttpUsesForwardedHeadersSetting()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(new ForwardedHeadersSetting { KnownNetworks = ["10.0.0.0/8"], KnownProxies = ["192.168.1.10"], ForwardLimit = 2 });

        // Act
        builder.ConfigureHttp();
        await using var provider = builder.Services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        Assert.Contains(IPNetwork.Parse("10.0.0.0/8"), options.KnownIPNetworks);
        Assert.Contains(options.KnownIPNetworks, static x => x.Contains(IPAddress.Loopback));
        Assert.Contains(IPAddress.Parse("192.168.1.10"), options.KnownProxies);
        Assert.Equal(2, options.ForwardLimit);
        Assert.False(provider.GetRequiredService<IOptions<KestrelServerOptions>>().Value.AddServerHeader);
    }

    // MudBlazor の文言は、UI の言語が英語でも日本語
    [Fact]
    public async Task ConfigureBlazorUsesJapaneseMudTexts()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.ConfigureBlazor();
        await using var provider = builder.Services.BuildServiceProvider();
        var interceptor = provider.GetRequiredService<ILocalizationInterceptor>();
        var culture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            // Act
            var text = interceptor.Handle("MudDataGridPager_RowsPerPage");

            // Assert
            Assert.Equal("表示件数:", text.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
