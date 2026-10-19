namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Smart.Data;

using Template.MobileServer.Accessors;
using Template.MobileServer.Services;
using Template.MobileServer.Web.Application.Context;
using Template.MobileServer.Web.Components.Pages;

public sealed class QrPageTests : MudBlazorTestBase
{
    [Fact]
    public void MakeEndPointUsesServerHostAndEndpointPort()
    {
        // Act
        var result = QrPage.MakeEndPoint("http://server:8080/", "http://*:9090");

        // Assert
        Assert.Equal("http://server:9090/", result);
    }

    [Fact]
    public void MakeEndPointUsesEndpointScheme()
    {
        // Act
        var result = QrPage.MakeEndPoint("https://server/", "http://*:9090");

        // Assert
        Assert.Equal("http://server:9090/", result);
    }

    [Fact]
    public void MakeEndPointWithoutConfigurationReturnsEmpty()
    {
        // Act
        var result = QrPage.MakeEndPoint("http://server:8080/", null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task SavedValuesAreStoredAndIncludedInQr()
    {
        // Arrange
        await using var keeper = OpenSharedMemoryDatabase("qr-save");
        var service = await AddSettingServiceAsync(keeper.ConnectionString);

        var cut = Render<QrPage>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("ApiEndPoint=http://localhost/\nGrpcEndPoint=http://localhost:9090/\nOtelEndPoint=http://localhost:4318/\n", cut.Find("pre.qr-text").TextContent, StringComparison.Ordinal));

        // Act
        await cut.Find("input[data-key='OllamaEndPoint']").InputAsync(new ChangeEventArgs { Value = "http://server:11434/" });
        await cut.WaitForAssertionAsync(() => Assert.Contains("OllamaEndPoint=http://server:11434/", cut.Find("pre.qr-text").TextContent, StringComparison.Ordinal));
        await cut.FindAll("button").First(static x => x.TextContent.Contains("保存", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

        // Assert
        var entity = Assert.Single(await service.QueryAllAsync(Xunit.TestContext.Current.CancellationToken));
        Assert.Equal("OllamaEndPoint", entity.Key);
        Assert.Equal("http://server:11434/", entity.Value);
    }

    [Fact]
    public async Task StoredValuesAreLoadedAndEmptyValueDeletesRow()
    {
        // Arrange
        await using var keeper = OpenSharedMemoryDatabase("qr-load");
        var service = await AddSettingServiceAsync(keeper.ConnectionString);
        using var scope = BeginSystemScope();
        await service.UpdateAsync("OllamaModel", "gemma2");

        // Act
        var cut = Render<QrPage>();

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Contains("OllamaModel=gemma2", cut.Find("pre.qr-text").TextContent, StringComparison.Ordinal));

        // Act
        await service.UpdateAsync("OllamaModel", " ");

        // Assert
        Assert.Empty(await service.QueryAllAsync(Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoopbackShowsWarning()
    {
        // Arrange
        await using var keeper = OpenSharedMemoryDatabase("qr-loopback");
        await AddSettingServiceAsync(keeper.ConnectionString);

        // Act
        var cut = Render<QrPage>();

        // Assert
        Assert.Contains("localhost で開いているため", cut.Find(".loopback-alert").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HostNameShowsNoWarning()
    {
        // Arrange
        await using var keeper = OpenSharedMemoryDatabase("qr-host");
        Services.AddSingleton<NavigationManager>(new HostNavigationManager());
        await AddSettingServiceAsync(keeper.ConnectionString);

        // Act
        var cut = Render<QrPage>();

        // Assert
        Assert.Empty(cut.FindAll(".loopback-alert"));
        await cut.WaitForAssertionAsync(() => Assert.Contains("ApiEndPoint=http://server:8080/\nGrpcEndPoint=http://server:9090/\nOtelEndPoint=http://server:4318/\n", cut.Find("pre.qr-text").TextContent, StringComparison.Ordinal));
    }

    private IDisposable BeginSystemScope() =>
        Services.GetRequiredService<ApplicationServiceContextProvider>().Begin(static () => new ServiceContext(TimeProvider.System.GetLocalNow(), "test"));

    private static SqliteConnection OpenSharedMemoryDatabase(string name)
    {
        var connection = new SqliteConnection($"Data Source=file:{name}?mode=memory&cache=shared");
        connection.Open();
        return connection;
    }

    private async Task<SettingService> AddSettingServiceAsync(string connectionString)
    {
        Services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
        Services.AddDataAccessors(typeof(SettingAccessor).Assembly);
        Services.AddSingleton<DatabaseService>();
        Services.AddSingleton<SettingService>();
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kestrel:Endpoints:Grpc:Url"] = "http://*:9090",
                ["Kestrel:Endpoints:OtelHttp:Url"] = "http://*:4318"
            })
            .Build());

        await Services.GetRequiredService<DatabaseService>().InitializeAsync("Assets/Data/Schema.sql");
        return Services.GetRequiredService<SettingService>();
    }

    private sealed class HostNavigationManager : NavigationManager
    {
        public HostNavigationManager()
        {
            Initialize("http://server:8080/", "http://server:8080/qr");
        }
    }
}
