namespace Template.MobileServer.Web.Application;

public static class ApplicationExtensions
{
    ////--------------------------------------------------------------------------------
    //// Configuration
    ////--------------------------------------------------------------------------------

    //public static IHostApplicationBuilder ConfigureConfigurationDefaults(this WebApplicationBuilder builder)
    //{
    //    return builder.ConfigureConfigurationDefaults(true);
    //}

    ////--------------------------------------------------------------------------------
    //// Logging
    ////--------------------------------------------------------------------------------

    //public static IHostApplicationBuilder ConfigureLogging(this IHostApplicationBuilder builder)
    //{
    //    builder.ConfigureLoggingDefaults(static options =>
    //    {
    //        options.Enrich.With(new CallbackEnricher(nameof(LoggingContext.RemoteIpAddress), () => LoggingContext.RemoteIpAddress ?? string.Empty));
    //        options.Enrich.With(new CallbackEnricher(nameof(LoggingContext.ClientId), () => LoggingContext.ClientId ?? string.Empty));
    //        options.Enrich.With(new CallbackEnricher(nameof(LoggingContext.UserId), () => LoggingContext.UserId ?? string.Empty));
    //    });

    //    return builder;
    //}

    //public static WebApplication UseLoggingContext(this WebApplication app)
    //{
    //    // For log
    //    app.UseMiddleware<LoggingContextMiddleware>();

    //    return app;
    //}

    ////--------------------------------------------------------------------------------
    //// API
    ////--------------------------------------------------------------------------------

    //public static IHostApplicationBuilder ConfigureApi(this IHostApplicationBuilder builder)
    //{
    //    builder.Services.AddSingleton<CredentialFilter>();
    //    builder.Services.AddSingleton(new CredentialFilterSetting
    //    {
    //        EnableSimulation = builder.Configuration.IsSimulationMode()
    //    });

    //    builder.ConfigureApiDefaults(static options =>
    //    {
    //        options.Filters.AddService<CredentialFilter>(Int32.MaxValue);
    //    });

    //    return builder;
    //}

    ////--------------------------------------------------------------------------------
    //// Swagger
    ////--------------------------------------------------------------------------------

    //public static IHostApplicationBuilder ConfigureSwagger(this IHostApplicationBuilder builder)
    //{
    //    if (!builder.Environment.IsProduction())
    //    {
    //        builder.Services.AddSingleton(new SwaggerSetting
    //        {
    //            IsClientIdDirect = !builder.Configuration.IsOpenApiOutputMode(),
    //            IsSimulationMode = builder.Configuration.IsSimulationMode()
    //        });

    //        builder.ConfigureSwaggerDefaults<Tags>(options =>
    //        {
    //            options.SwaggerDoc("xxx", new OpenApiInfo { Title = "xxx/API", Version = "0.1.0" });

    //            // Custom
    //            options.DocumentFilter<ApplicationDocumentFilter>();
    //            options.OperationFilter<CredentialOperationFilter>();

    //            // Order
    //            options.OrderActionsBy(api =>
    //            {
    //                var area = api.ActionDescriptor.RouteValues["area"];
    //                var controller = api.ActionDescriptor.RouteValues["controller"];
    //                return $"{AreaOrders.GetOrder(area)}_{controller}";
    //            });
    //        }, ["Template.Api.Areas.Default", "Template.Api.Areas"]);
    //    }

    //    return builder;
    //}

    //public static WebApplication UseSwagger(this WebApplication app)
    //{
    //    if (!app.Environment.IsProduction())
    //    {
    //        app.UseSwaggerDefaults(static options =>
    //        {
    //            options.SwaggerEndpoint("xxx/swagger.json", "xxx/API");
    //        });
    //    }

    //    return app;
    //}

    ////--------------------------------------------------------------------------------
    //// Components
    ////--------------------------------------------------------------------------------

    //public static IHostApplicationBuilder ConfigureComponent(this IHostApplicationBuilder builder)
    //{
    //    // Setting
    //    builder.Services.Configure<LimitSetting>(builder.Configuration.GetSection("Limit"));
    //    builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<LimitSetting>>().Value);
    //    builder.Services.Configure<ReportSetting>(builder.Configuration.GetSection("Report"));
    //    builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<ReportSetting>>().Value);

    //    // Default
    //    builder.ConfigureComponentDefaults(static (p, c) => c.AddFragmentProfile(p));

    //    return builder;
    //}

    //[MapConfigExtension]
    //public static partial void AddFragmentProfile(this IMapperConfigurationExpression expression, IServiceProvider provider);

    ////--------------------------------------------------------------------------------
    //// Configuration
    ////--------------------------------------------------------------------------------

    //private static bool IsSimulationMode(this IConfiguration configuration) =>
    //    Boolean.TryParse(configuration["SIMULATION_MODE"], out var value) && value;

    ////--------------------------------------------------------------------------------
    //// Startup
    ////--------------------------------------------------------------------------------

    //public static void LogStartupInformation(this WebApplication app)
    //{
    //    ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
    //    app.Logger.InfoServiceStart();
    //    app.Logger.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
    //    app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.CurrentDirectory);
    //    app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
    //    app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);
    //    app.Logger.InfoServiceSettingsTelemetry(app.Configuration.GetOtelExporterEndpoint(), app.Configuration.IsPrometheusExporterEnabled());
    //    app.Logger.InfoServiceSettingsMode(app.Configuration.IsSimulationMode());
    //}

    ////--------------------------------------------------------------------------------
    //// Startup
    ////--------------------------------------------------------------------------------

    //public static async ValueTask InitializeApplicationAsync(this WebApplication app)
    //{
    //    var calendarManager = app.Services.GetRequiredService<ICalenderManager>();
    //    await calendarManager.LoadCalendarHolidayAsync();
    //}
}
