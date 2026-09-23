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
    public void MakeGrpcEndPointUsesServerHostAndGrpcPort()
    {
        // Act
        var result = QrPage.MakeGrpcEndPoint("http://server:8080/", "http://*:9090");

        // Assert
        Assert.Equal("http://server:9090/", result);
    }

    [Fact]
    public void MakeGrpcEndPointWithoutConfigurationReturnsEmpty()
    {
        // Act
        var result = QrPage.MakeGrpcEndPoint("http://server:8080/", null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    // 保存: 入力した値が Setting テーブルに保存され、QR の生成値に含まれる。接続先 (API / gRPC / OTEL) はサーバー自身のホスト (gRPC / OTEL は Kestrel のポート)
    [Fact]
    public async Task SavedValuesAreStoredAndIncludedInQr()
    {
        // Arrange
        await using var keeper = OpenSharedMemoryDatabase("qr-save");
        var service = await AddSettingServiceAsync(keeper.ConnectionString);

        var cut = Render<QrPage>();
        await cut.WaitForAssertionAsync(() => Assert.Contains("ApiEndPoint=http://localhost/\nGrpcEndPoint=http://localhost:9090/\nOtelEndPoint=http://localhost:4317/\n", cut.Find("pre.qr-text").TextContent, StringComparison.Ordinal));

        // Act
        await cut.Find("input[data-key='OllamaEndPoint']").InputAsync(new ChangeEventArgs { Value = "http://server:11434/" });
        await cut.WaitForAssertionAsync(() => Assert.Contains("OllamaEndPoint=http://server:11434/", cut.Find("pre.qr-text").TextContent, StringComparison.Ordinal));
        await cut.FindAll("button").First(static x => x.TextContent.Contains("保存", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

        // Assert
        var entity = Assert.Single(await service.QueryAllAsync(Xunit.TestContext.Current.CancellationToken));
        Assert.Equal("OllamaEndPoint", entity.Key);
        Assert.Equal("http://server:11434/", entity.Value);
    }

    // 読込: 保存済みの値が表示され、空の値は削除される
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

    // 画面を経由しない直接の呼び出しはワーカーと同じく明示的にスコープを開始する
    private IDisposable BeginSystemScope() =>
        Services.GetRequiredService<ApplicationServiceContextProvider>().Begin(static () => new ServiceContext(TimeProvider.System.GetLocalNow(), "test"));

    // 共有キャッシュのインメモリ DB。接続を 1 つ開いたままにして生存させる
    private static SqliteConnection OpenSharedMemoryDatabase(string name)
    {
        var connection = new SqliteConnection($"Data Source=file:{name}?mode=memory&cache=shared");
        connection.Open();
        return connection;
    }

    // スキーマは Web の Assets/Data/Schema.sql (参照プロジェクトから出力先へコピーされる)。Kestrel のポートは appsettings.json と同じ
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
                ["Kestrel:Endpoints:Otel:Url"] = "http://*:4317"
            })
            .Build());

        await Services.GetRequiredService<DatabaseService>().InitializeAsync("Assets/Data/Schema.sql", Xunit.TestContext.Current.CancellationToken);
        return Services.GetRequiredService<SettingService>();
    }
}
