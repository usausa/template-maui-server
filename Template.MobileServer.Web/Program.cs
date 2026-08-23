using Microsoft.Extensions.Hosting.WindowsServices;

using Template.MobileServer.Web.Application;

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
// Authentication
builder.ConfigureAuthentication();
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

// Error handler
app.UseErrorHandler();

// Logging
app.UseLogging();

// Authentication
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Logging context
app.UseLoggingContext();

// End point
app.MapEndpoints();

// Initialize
await app.InitializeApplicationAsync();

// Run
await app.RunAsync();

[ExcludeFromCodeCoverage]
public partial class Program
{
}
