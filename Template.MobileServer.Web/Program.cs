using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using Microsoft.AspNetCore.HttpLogging;

using MudBlazor.Services;

using Serilog;

using Smart.AspNetCore.ApplicationModels;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Log
builder.Logging.ClearProviders();
builder.Services.AddSerilog(option => option.ReadFrom.Configuration(builder.Configuration), writeToProviders: true);

// TODO
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody;

    options.MediaTypeOptions.AddText("application/json");
    options.MediaTypeOptions.AddText("application/*+json");

    options.RequestBodyLogLimit = 16 * 1024;
    options.ResponseBodyLogLimit = 16 * 1024;
    options.CombineLogs = true;
});

// Add framework Services.
builder.Services.AddHttpContextAccessor();

// Route
builder.Services.Configure<RouteOptions>(options =>
{
    options.AppendTrailingSlash = true;
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controller
builder.Services
    .AddControllers(options =>
    {
        options.Conventions.Add(new LowercaseControllerModelConvention());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.JsonSerializerOptions.Converters.Add(new Template.MobileServer.Components.Json.DateTimeConverter());
    });

// Open API
builder.Services.AddOpenApi();

// TODO
// Rate limit
//var rateLimitSetting = builder.Configuration.GetSection("RateLimit").Get<RateLimitSetting>()!;
//builder.Services.AddRateLimiter(config =>
//{
//    config.AddFixedWindowLimiter(LimitPolicy.Gateway, options =>
//    {
//        options.Window = TimeSpan.FromMilliseconds(rateLimitSetting.Window);
//        options.PermitLimit = rateLimitSetting.PermitLimit;
//        options.QueueLimit = rateLimitSetting.QueueLimit;
//    });
//    config.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//});

// Error handler
builder.Services.AddProblemDetails();

// Service
// TODO
//builder.Services.AddSingleton<DataService>();

// Component
// TODO

// State
// TODO
//builder.Services.AddScoped<SessionState>();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline.
//--------------------------------------------------------------------------------
var app = builder.Build();

app.MapOpenApi("/swagger/v1/swagger.json");
app.UseSwaggerUI();

// TODO
// Logging
app.UseWhen(
    c => c.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    b => b.UseHttpLogging());

// Error handler
app.UseWhen(
    c => c.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    b => b.UseExceptionHandler());

// Security
app.UseAntiforgery();

// Rate limit
// TODO
//app.UseRateLimiter();

// Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// API
app.MapControllers();

app.Run();
