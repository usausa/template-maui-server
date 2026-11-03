namespace Template.MobileServer.Application;

using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MudBlazor;

using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Settings;

public sealed class ApplicationExtensionsTests
{
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

    [Fact]
    public async Task ConfigureSignalRUsesSettings()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SignalR:KeepAliveInterval"] = "00:00:20",
            ["SignalR:ClientTimeoutInterval"] = "00:00:40"
        });

        // Act
        builder.ConfigureSignalR();
        await using var provider = builder.Services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<HubOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(20), options.KeepAliveInterval);
        Assert.Equal(TimeSpan.FromSeconds(40), options.ClientTimeoutInterval);
    }

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

    [Fact]
    public async Task ConfigureBlazorUsesCircuitAndHubSettings()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Blazor:Circuit:DisconnectedCircuitRetentionPeriod"] = "00:10:00",
            ["Blazor:Circuit:PersistedCircuitInMemoryRetentionPeriod"] = "04:00:00",
            ["Blazor:Hub:MaximumReceiveMessageSize"] = "65536"
        });

        // Act
        builder.ConfigureBlazor();
        await using var provider = builder.Services.BuildServiceProvider();

        // Assert
        var circuit = provider.GetRequiredService<IOptions<CircuitOptions>>().Value;
        Assert.Equal(TimeSpan.FromMinutes(10), circuit.DisconnectedCircuitRetentionPeriod);
        Assert.Equal(TimeSpan.FromHours(4), circuit.PersistedCircuitInMemoryRetentionPeriod);
        Assert.Equal(65536, ResolveComponentHubOptions(provider).MaximumReceiveMessageSize);
    }

    private static HubOptions ResolveComponentHubOptions(IServiceProvider services)
    {
        var hubType = typeof(CircuitOptions).Assembly.GetType("Microsoft.AspNetCore.Components.Server.ComponentHub", throwOnError: true)!;
        var optionsType = typeof(IOptions<>).MakeGenericType(typeof(HubOptions<>).MakeGenericType(hubType));
        var options = services.GetRequiredService(optionsType);
        return (HubOptions)optionsType.GetProperty(nameof(IOptions<>.Value))!.GetValue(options)!;
    }
}
