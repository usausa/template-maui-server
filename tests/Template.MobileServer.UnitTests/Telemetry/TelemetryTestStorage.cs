namespace Template.MobileServer.Telemetry;

using System.Data.Common;
using System.Text.RegularExpressions;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Smart.Data;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Telemetry;
using Template.MobileServer.Services;
using Template.MobileServer.Web.Application.Context;
using Template.MobileServer.Web.Telemetry;

public sealed class TelemetryTestStorage : IDisposable
{
    private readonly SqliteConnection keeper;

    private readonly ServiceProvider provider;

    public string Root { get; }

    public TelemetryService Service => provider.GetRequiredService<TelemetryService>();

    public TelemetryStore Store => provider.GetRequiredService<TelemetryStore>();

    public DataService DataService => provider.GetRequiredService<DataService>();

    public DeviceService DeviceService => provider.GetRequiredService<DeviceService>();

    public PushService PushService => provider.GetRequiredService<PushService>();

    public SettingService SettingService => provider.GetRequiredService<SettingService>();

    public TelemetryBus Bus => provider.GetRequiredService<TelemetryBus>();

    public TelemetryDeviceRegistry Registry => provider.GetRequiredService<TelemetryDeviceRegistry>();

    public ITelemetryDbProvider DbProvider => provider.GetRequiredService<ITelemetryDbProvider>();

    public TelemetryTestStorage()
    {
        Root = Path.Combine(Path.GetTempPath(), "template-telemetry-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);

        var connectionString = $"Data Source=file:telemetry-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";
        keeper = new SqliteConnection(connectionString);
        keeper.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
        services.AddSingleton<IDialect>(new DelegateDialect(static _ => false, static x => Regex.Replace(x, @"[%_\\]", @"\$0")));
        services.AddDataAccessors(typeof(TelemetryAccessor).Assembly);
        services.AddSingleton(new TelemetryStorageOption { Root = Root });
        services.AddSingleton<ITelemetryDbProvider, SqliteTelemetryDbProvider>();
        services.AddSingleton<ApplicationServiceContextProvider>();
        services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<DataService>();
        services.AddSingleton<DeviceService>();
        services.AddSingleton<PushService>();
        services.AddSingleton<SettingService>();
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<TelemetryStore>();
        services.AddSingleton<TelemetryBus>();
        services.AddSingleton<TelemetryDeviceRegistry>();
        provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        provider.Dispose();
        keeper.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(Root, true);
    }

    public ValueTask PrepareDatabaseAsync() =>
        provider.GetRequiredService<DatabaseService>().InitializeAsync("Assets/Data/Schema.sql");

    public IDisposable BeginScope(DateTimeOffset now) =>
        provider.GetRequiredService<ApplicationServiceContextProvider>().Begin(() => new ServiceContext(now, "test"));

    public async ValueTask<object?> QueryDataValueAsync(Action<DbCommand> prepare)
    {
        await using var con = provider.GetRequiredService<IDbProvider>().CreateConnection();
        await con.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = con.CreateCommand();
        prepare(command);
        return await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask<object?> QueryValueAsync(string deviceId, Action<DbCommand> prepare)
    {
        await using var con = await DbProvider.OpenExistingAsync(deviceId, TestContext.Current.CancellationToken);
        Assert.NotNull(con);
        await using var command = con.CreateCommand();
        prepare(command);
        return await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
    }
}
