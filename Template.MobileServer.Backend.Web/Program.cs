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
using Smart.Data.Accessor.Extensions.DependencyInjection;
using Smart.Data;
using Smart.Data.Accessor;

using Template.MobileServer.Backend.Components.Storage;
using Template.MobileServer.Backend.Accessor;
using Template.MobileServer.Backend.Web;

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

// TODO Settings

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

// Add services to the container.
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
builder.Services.AddRazorPages();

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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Health
builder.Services.AddHealthChecks();

// Swagger
builder.Services.AddSwaggerGen();

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

// Mapper
builder.Services.AddSingleton<IMapper>(new Mapper(new MapperConfiguration(c =>
{
    c.AddProfile<MappingProfile>();
})));

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

// Startup information
ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
app.Logger.InfoServiceStart();
app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.Version, Environment.CurrentDirectory);
app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);

// Prepare
if (!File.Exists(connectionStringBuilder.DataSource))
{
    var accessor = app.Services.GetRequiredService<IAccessorResolver<IDataAccessor>>().Accessor;
    accessor.Create();
    await accessor.InsertAsync(new DataEntity { Id = 1, Name = "Data-1" });
    await accessor.InsertAsync(new DataEntity { Id = 2, Name = "Data-2" });
    await accessor.InsertAsync(new DataEntity { Id = 3, Name = "Data-3" });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// TODO auth?

// Static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Health
app.UseHealthChecks("/health");

// TODO Metrics
//app.UseHttpMetrics();

// Routing
app.UseRouting();

// TODO Metrics
//app.MapMetrics();

// Compression
app.UseRequestDecompression();
app.UseResponseCompression();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

// Initialize
await app.InitializeAsync();

// Run
await app.RunAsync();
