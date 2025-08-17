using System.IO.Compression;
using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting.WindowsServices;

using MudBlazor;
using MudBlazor.Services;

using Serilog;

using Smart.AspNetCore;
using Smart.AspNetCore.ApplicationModels;
using Smart.Data;
using Smart.Data.Accessor;
using Smart.Data.Accessor.Extensions.DependencyInjection;

using Template.MobileServer.Backend.Accessor;
using Template.MobileServer.Backend.Components.Storage;
using Template.MobileServer.Backend.Web;
using Template.MobileServer.Backend.Web.Application;
using Template.MobileServer.Backend.Web.Components;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// Service
builder.Host
    .UseWindowsService()
    .UseSystemd();

// Configuration
//var serverSetting = builder.Configuration.GetSection("Server").Get<ServerSetting>()!;
//builder.Services.AddSingleton(serverSetting);

// Log
builder.Logging.ClearProviders();
builder.Host
    .UseSerilog(static (hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    });

// Add framework Services.
builder.Services.AddHttpContextAccessor();

// Route
builder.Services.Configure<RouteOptions>(static options =>
{
    options.AppendTrailingSlash = true;
});

// API
builder.Services.AddTimeLogging(static options =>
{
    options.Threshold = 10_000;
});

// Controller
builder.Services
    .AddControllersWithViews(static options =>
    {
        options.Filters.AddTimeLogging();
        options.Conventions.Add(new LowercaseControllerModelConvention());
    })
    .AddJsonOptions(static options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.JsonSerializerOptions.Converters.Add(new Template.MobileServer.Backend.Components.Json.DateTimeConverter());
    });

// Add MudBlazor services
builder.Services.AddMudServices(static config =>
{
    // Snackbar
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// TODO
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Health
builder.Services.AddHealthChecks();

// TODO
// Swagger
//builder.Services.AddSwaggerGen();

// Compression
builder.Services.AddRequestDecompression();
builder.Services.AddResponseCompression(static options =>
{
    // Default false (for CRIME and BREACH attacks)
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = [MediaTypeNames.Application.Json];
});
builder.Services.Configure<GzipCompressionProviderOptions>(static options =>
{
    options.Level = CompressionLevel.Fastest;
});

// TODO Validation

// HTTP
builder.Services.AddHttpClient();

// Data
var connectionStringBuilder = new SqliteConnectionStringBuilder
{
    DataSource = "Data.db",
    Pooling = true,
    Cache = SqliteCacheMode.Shared
};
var connectionString = connectionStringBuilder.ConnectionString;
builder.Services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
builder.Services.AddSingleton<IDialect>(new DelegateDialect(
    static ex => ex is SqliteException { SqliteErrorCode: 1555 },
    static x => Regex.Replace(x, "[%_]", "[$0]")));
builder.Services.AddDataAccessor();

// TODO Mapper
//builder.Services.AddSingleton<IMapper>(new Mapper(new MapperConfiguration(c =>
//{
//    c.AddProfile<MappingProfile>();
//})));

// Storage
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<FileStorageOptions>>().Value);
builder.Services.AddSingleton<IStorage, FileStorage>();

// Service
builder.Services.AddSingleton<DataService>();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline
//--------------------------------------------------------------------------------
var app = builder.Build();

// TODO 見直し
// Startup information
ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
app.Logger.InfoServiceStart();
app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.Version, Environment.CurrentDirectory);
app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);

// TODO 見直し
// Prepare
if (!File.Exists(connectionStringBuilder.DataSource))
{
    var accessor = app.Services.GetRequiredService<IAccessorResolver<IDataAccessor>>().Accessor;
    accessor.Create();
    await accessor.InsertAsync(new DataEntity { Id = 1, Name = "Data-1" });
    await accessor.InsertAsync(new DataEntity { Id = 2, Name = "Data-2" });
    await accessor.InsertAsync(new DataEntity { Id = 3, Name = "Data-3" });
}

// TODO 見直し https,swagger
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseWebAssemblyDebugging();
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//else
//{
//    app.UseHsts();
//}

// TODO 見直し
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// TODO 順序
// Health
app.UseHealthChecks("/health");

// TODO Metrics
//app.UseHttpMetrics();

// TODO auth?

// TODO 順序
// Security
app.UseAntiforgery();

// TODO Metrics
//app.MapMetrics();

// TODO 順序
// Compression
app.UseRequestDecompression();
app.UseResponseCompression();

// API
app.MapControllers();

// Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Initialize
await app.InitializeAsync();

// Run
await app.RunAsync();

// TODO swagger
// TODO open telemetry
// TODO aspire
// TODO controller up from work
// TODO signalR
// TODO grpc
