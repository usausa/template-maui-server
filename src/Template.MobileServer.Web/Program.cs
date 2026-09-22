using Microsoft.Extensions.Hosting.WindowsServices;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// System
builder.ConfigureSystem();

// Host
builder.ConfigureHost();

// Logging
builder.ConfigureLogging();

// Http
builder.ConfigureHttp();
// API
builder.ConfigureApi();
// gRPC
builder.ConfigureGrpc();
// SignalR
builder.ConfigureSignalR();
// Authentication
builder.ConfigureAuthentication();
// Compress
builder.ConfigureCompression();
// OpenApi
builder.ConfigureOpenApi();

// Blazor
builder.ConfigureBlazor();

// Health
builder.ConfigureHealth();
// Metrics
builder.ConfigureTelemetry();

// Components
builder.ConfigureComponents();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline.
//--------------------------------------------------------------------------------
var app = builder.Build();

// Startup information
app.LogStartupInformation();

// Forwarded headers
app.UseForwardedHeaders();

// Security headers
app.UseSecurityHeaders();

// W3C log
app.UseW3CLog();

// Error handler
app.UseErrorHandler();

// Routing
app.UseRouting();

// Compression
app.UseCompression();

// HTTP log
app.UseHttpLog();

// Authentication
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// End point
app.MapEndpoints();

// Initialize
await app.InitializeApplicationAsync();

// Run
await app.RunAsync();

[ExcludeFromCodeCoverage]
public partial class Program;
