namespace Template.MobileServer.Web.Application;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;

using MiniDataProfiler;
using MiniDataProfiler.Listener.Logging;
using MiniDataProfiler.Listener.OpenTelemetry;

using MudBlazor;
using MudBlazor.Services;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

using Smart.Data;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Application.Authentication;
using Template.MobileServer.Web.Application.Context;
using Template.MobileServer.Web.Application.ExceptionHandling;
using Template.MobileServer.Web.Application.HealthChecks;
using Template.MobileServer.Web.Application.Telemetry;
using Template.MobileServer.Web.Components;
using Template.MobileServer.Web.Endpoints;
using Template.MobileServer.Web.Handlers;
using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Infrastructure.Logging;
using Template.MobileServer.Web.Infrastructure.Routing;
using Template.MobileServer.Web.Infrastructure.Security;

public static class ApplicationExtensions
{
    private const string GrpcEndpointConfigurationKey = "Kestrel:Endpoints:Grpc:Url";

    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    private const string SchemaPath = "Assets/Data/Schema.sql";

    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureSystem(this WebApplicationBuilder builder)
    {
        // Path
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        // Encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Host
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
    {
        // Service
        builder.Services
            .AddWindowsService()
            .AddSystemd();

        // Feature management
        builder.Services.AddFeatureManagement();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Logging
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureLogging(this IHostApplicationBuilder builder)
    {
        var setting = builder.Configuration.GetSection("Log").Get<LogSetting>()!;
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        // Application log
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(
            (provider, options) =>
            {
                var accessor = provider.GetRequiredService<IHttpContextAccessor>();
                options.ReadFrom.Configuration(builder.Configuration);
                options.Enrich.With(new CallbackEnricher("RemoteIpAddress", () => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString()));
                options.Enrich.With(new CallbackEnricher("UserId", () => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)));
            },
            writeToProviders: useOtlpExporter);

        // HTTP log
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod |
                                    HttpLoggingFields.RequestPath |
                                    HttpLoggingFields.ResponseStatusCode |
                                    HttpLoggingFields.Duration;
            if (setting.HttpDump)
            {
                options.LoggingFields |= HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody;
                options.CombineLogs = true;
                options.RequestBodyLogLimit = setting.HttpDumpLimit;
                options.ResponseBodyLogLimit = setting.HttpDumpLimit;
                options.MediaTypeOptions.Clear();
                options.MediaTypeOptions.AddText("application/json");
                options.MediaTypeOptions.AddText("application/*+json");
                options.MediaTypeOptions.AddText("application/xml");
                options.MediaTypeOptions.AddText("application/*+xml");
            }
        });

        // Access log (W3C)
        if (setting.W3CLog.Enable)
        {
            builder.Services.AddW3CLogging(options =>
            {
                options.LogDirectory = setting.W3CLog.Directory;
                options.FileName = setting.W3CLog.FileName;
                options.RetainedFileCountLimit = setting.W3CLog.RetainedFileCount;
            });
        }

        return builder;
    }

    public static WebApplication UseW3CLog(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<LogSetting>();
        if (setting.W3CLog.Enable)
        {
            app.UseW3CLogging();
        }

        return app;
    }

    public static WebApplication UseHttpLog(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<LogSetting>();
        if (setting.HttpLog)
        {
            app.UseWhen(
                static context => context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
                static b => b.UseHttpLogging());
        }

        return app;
    }

    //--------------------------------------------------------------------------------
    // Http
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHttp(this IHostApplicationBuilder builder)
    {
        // Add services to the container
        builder.Services.AddHttpContextAccessor();

        // CSP nonce
        builder.Services.AddScoped<CspNonce>();

        // XForward
        builder.Services.Configure<ForwardedHeadersOptions>(static options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Do not restrict to local network/proxy
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return builder;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        // HSTS
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Headers. The nonce admits the import map that Blazor renders inline, MudBlazor needs inline styles,
        // dotnet watch / Browser Link load their script from another localhost port and connect back to it
        var development = app.Environment.IsDevelopment();
        var scriptSources = development ? "'self' http://localhost:*" : "'self'";
        var connectSources = development ? "'self' http://localhost:* ws://localhost:* wss://localhost:*" : "'self'";
        app.UseMiddleware<SecurityHeadersMiddleware>(new SecurityHeadersOption
        {
            ReportOnly = app.Services.GetRequiredService<CspSetting>().ReportOnly,
            ContentSecurityPolicy = $"default-src 'self'; base-uri 'self'; object-src 'none'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; font-src 'self'; style-src 'self' 'unsafe-inline'; script-src {scriptSources} 'nonce-{{nonce}}'; connect-src {connectSources}"
        });

        return app;
    }

    //--------------------------------------------------------------------------------
    // API
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureApi(this IHostApplicationBuilder builder)
    {
        // JSON (camelCase。クライアントの Rester 既定と同じ)
        builder.Services.ConfigureHttpJsonOptions(static options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = NamingPolicy.JsonPropertyNaming;
            options.SerializerOptions.DictionaryKeyPolicy = NamingPolicy.JsonDictionaryKeyNaming;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            options.SerializerOptions.Converters.Add(new Template.MobileServer.Infrastructure.Json.DateTimeConverter());
        });

        // Validation
        builder.Services.AddValidation();

        // Error handler
        builder.Services.AddProblemDetails(static options =>
        {
            options.CustomizeProblemDetails = static context =>
            {
                context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            };
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplication UseErrorHandler(this WebApplication app)
    {
        // API: ProblemDetails
        app.UseWhen(
            static context => context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
            static b =>
            {
                b.UseExceptionHandler();
                b.UseStatusCodePages();
            });

        // Page: error page
        app.UseWhen(
            static context => !context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
            static b =>
            {
                b.UseExceptionHandler("/error", createScopeForErrors: true);
            });

        return app;
    }

    //--------------------------------------------------------------------------------
    // gRPC
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureGrpc(this IHostApplicationBuilder builder)
    {
        // gRPC
        builder.Services.AddGrpc(static options =>
        {
            options.Interceptors.Add<ServiceContextInterceptor>();
        });

        // Policy
        builder.Services.AddSingleton<MatcherPolicy, PortMatcherPolicy>();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // SignalR
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureSignalR(this IHostApplicationBuilder builder)
    {
        // SignalR
        builder.Services.AddSignalR(static options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

            // Filter
            options.AddFilter<ServiceContextHubFilter>();
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Authentication
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureAuthentication(this IHostApplicationBuilder builder)
    {
        var jwtSetting = builder.Configuration.GetSection("Jwt").Get<JwtSetting>()!;

        // モバイルAPI用の JWT Bearer のみ(管理画面は認証なし)
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSetting.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSetting.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        builder.Services.AddAuthorization(static options =>
        {
            options.AddPolicy(Policies.MobileApi, new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Compress
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureCompression(this IHostApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(static options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        builder.Services.AddRequestDecompression();

        return builder;
    }

    public static WebApplication UseCompression(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<CompressionSetting>();
        if (setting.Response || setting.Request)
        {
            app.UseWhen(
                static context => context.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase),
                b =>
                {
                    if (setting.Response)
                    {
                        b.UseResponseCompression();
                    }

                    if (setting.Request)
                    {
                        b.UseRequestDecompression();
                    }
                });
        }

        return app;
    }

    //--------------------------------------------------------------------------------
    // OpenApi
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureOpenApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(static options =>
        {
            options.AddDocumentTransformer(static (document, _, _) =>
            {
                document.Info.Title = "Template API";
                document.Info.Version = "v1";
                document.Info.Description = "Template mobile server API.";
                return Task.CompletedTask;
            });
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Blazor
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureBlazor(this IHostApplicationBuilder builder)
    {
        // Razor components
        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });

        // Error boundary logging
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger, ErrorBoundaryLogger>();

        // Circuit tracking
        builder.Services.AddSingleton<Circuits.CircuitTracker>();
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, Circuits.AppCircuitHandler>();

        // MudBlazor
        builder.Services.AddMudServices(static options =>
        {
            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            options.SnackbarConfiguration.PreventDuplicates = true;
            options.SnackbarConfiguration.NewestOnTop = false;
            options.SnackbarConfiguration.ShowCloseIcon = true;
            options.SnackbarConfiguration.VisibleStateDuration = 5000;
            options.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Health
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHealth(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck<DatabaseHealthCheck>("database");

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Telemetry
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureTelemetry(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        var prometheusSection = builder.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue<string>("Uri")!;
        var usePrometheusExporter = !String.IsNullOrEmpty(prometheusUri);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(config =>
            {
                config.AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName);
            });

        // Log
        if (useOtlpExporter)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
            builder.Services.Configure<OpenTelemetryLoggerOptions>(static logging =>
            {
                logging.AddOtlpExporter();
            });
        }

        // Metrics
        if (useOtlpExporter || usePrometheusExporter)
        {
            telemetry
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddApplicationInstrumentation();

                    if (useOtlpExporter)
                    {
                        metrics.AddOtlpExporter();
                    }

                    if (usePrometheusExporter)
                    {
                        var prometheusEndpoint = new Uri(prometheusUri);
                        metrics.AddPrometheusHttpListener(config =>
                        {
                            config.Host = prometheusEndpoint.Host;
                            config.Port = prometheusEndpoint.Port;
                        });
                    }
                });
        }

        // Trace
        if (useOtlpExporter)
        {
            telemetry
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(static options =>
                        {
                            options.Filter = static context =>
                            {
                                var path = context.Request.Path;
                                return !path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/redoc", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddMiniDataProfilerInstrumentation()
                        .AddApplicationInstrumentation();

                    tracing.AddOtlpExporter();
                });
        }

        // Custom instrument
        builder.Services.AddApplicationInstrument();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureComponents(this IHostApplicationBuilder builder)
    {
        // System
        builder.Services.AddSingleton(TimeProvider.System);

        // Data
        builder.Services.AddSingleton<IDbProvider>(static p =>
        {
            var configuration = p.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Default");

            var listener = CreateProfileListener(p, p.GetRequiredService<ProfilerSetting>());
            if (listener is not null)
            {
                return new DelegateDbProvider(() => new ProfileDbConnection(listener, new SqliteConnection(connectionString)));
            }

            return new DelegateDbProvider(() => new SqliteConnection(connectionString));
        });
        builder.Services.AddSingleton<IDialect>(new DelegateDialect(
            static ex => ex is SqliteException { SqliteErrorCode: 19 } or SqliteException { SqliteExtendedErrorCode: 1555 or 2067 },
            static x => Regex.Replace(x, "[%_]", "[$0]")));
        builder.Services.AddDataAccessors(typeof(DataAccessor).Assembly);

        // Cache
        builder.Services.AddMemoryCache();

        // Storage
        builder.Services.AddOptions<FileStorageOption>().BindConfiguration("Storage").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<FileStorageOption>>().Value);
        builder.Services.AddSingleton<IStorage, FileStorage>();

        // Security
        builder.Services.AddOptions<JwtSetting>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<JwtSetting>>().Value);
        builder.Services.AddSingleton<JwtTokenProvider>();

        // Service
        builder.Services.AddSingleton<ApplicationServiceContextProvider>();
        builder.Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        builder.Services.AddScoped<BlazorServiceScope>();

        builder.Services.AddCoreServices();

        // Notification
        builder.Services.AddSingleton<Infrastructure.Notifications.NotificationBus>();
        builder.Services.AddOptions<Workers.NotificationWorkerOption>().BindConfiguration("Notification").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<Workers.NotificationWorkerOption>>().Value);
        builder.Services.AddHostedService<Workers.NotificationWorker>();

        // Chat
        builder.Services.AddSingleton<Services.ChatService>();

        // Monitor
        builder.Services.AddSingleton<Services.DeviceRegistry>();
        builder.Services.AddSingleton<Services.MonitorNotifier>();
        builder.Services.AddOptions<Workers.ServerStatusWorkerOption>().BindConfiguration("ServerStatus").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<Workers.ServerStatusWorkerOption>>().Value);
        builder.Services.AddHostedService<Workers.ServerStatusWorker>();

        // Setting
        builder.Services.AddOptions<CompressionSetting>().BindConfiguration("Compression").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<CompressionSetting>>().Value);
        builder.Services.AddOptions<CspSetting>().BindConfiguration("Csp").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<CspSetting>>().Value);
        builder.Services.AddOptions<LogSetting>().BindConfiguration("Log").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<LogSetting>>().Value);
        builder.Services.AddOptions<ProfilerSetting>().BindConfiguration("Profiler").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<ProfilerSetting>>().Value);
        builder.Services.AddOptions<TelemetrySetting>().BindConfiguration("Telemetry").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<TelemetrySetting>>().Value);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Information
    //--------------------------------------------------------------------------------

    public static void LogStartupInformation(this WebApplication app)
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        var prometheusSection = app.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue("Uri", string.Empty);
        var version = typeof(Program).Assembly.GetName().Version;
        var otelEndpoint = app.Configuration.GetOtelExporterEndpoint();

        app.Logger.InfoServiceStart();
        app.Logger.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        app.Logger.InfoServiceSettingsEnvironment(version, Environment.CurrentDirectory);
        app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);
        app.Logger.InfoServiceSettingsTelemetry(otelEndpoint, prometheusUri);
    }

    //--------------------------------------------------------------------------------
    // End point
    //--------------------------------------------------------------------------------

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Develop
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // [MEMO] Add yaml support
            app.MapOpenApi("/openapi/{documentName}.yaml");

            // NSwag UI (SwaggerUI / ReDoc) using MapOpenApi generated specification
            app.UseSwaggerUi(static options =>
            {
                options.DocumentPath = "/openapi/v1.json";
            });
            app.UseReDoc(static options =>
            {
                options.Path = "/redoc";
                options.DocumentPath = "/openapi/v1.json";
            });
        }

        // Static assets
        app.MapStaticAssets();

        // Blazor
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode(static options =>
            {
                options.ContentSecurityFrameAncestorsPolicy = "'none'";
            });

        // API (モバイル契約)
        app.MapServerEndpoints();
        app.MapAccountEndpoints();
        app.MapSecretEndpoints();
        app.MapDataEndpoints();
        app.MapStorageEndpoints();
        app.MapTestEndpoints();

        // gRPC (チャット / サーバー情報、認証なし。アプリケーション用ポートのみ)
        var grpcPort = GetEndpointPort(app.Configuration, GrpcEndpointConfigurationKey);
        app.MapGrpcService<ChatHandler>().RequirePort(grpcPort);
        app.MapGrpcService<ServerInfoHandler>().RequirePort(grpcPort);

        // SignalR (端末の監視、認証なし)
        app.MapHub<MonitorHub>(HubRoutes.Monitor);

        // Health
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = static r => r.Tags.Contains("live")
        });

        return app;
    }

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static ValueTask InitializeApplicationAsync(this WebApplication app)
    {
        // Prepare instrument
        app.Services.GetRequiredService<ApplicationInstrument>();

        // Prepare storage
        Directory.CreateDirectory(app.Services.GetRequiredService<FileStorageOption>().Root);

        // Prepare notifier
        app.Services.GetRequiredService<Services.MonitorNotifier>();

        // Prepare database (schema from the SQL file)
        return app.Services.GetRequiredService<DatabaseService>().InitializeAsync(SchemaPath, CancellationToken.None);
    }

    //--------------------------------------------------------------------------------
    // Profiler
    //--------------------------------------------------------------------------------

    private static IProfileListener? CreateProfileListener(IServiceProvider provider, ProfilerSetting setting)
    {
        var listeners = new List<IProfileListener>();
        if (setting.SqlLog.Enable)
        {
            var option = new LoggingListenerOption
            {
                OutputParameter = setting.SqlLog.OutputParameter,
                ElapsedThreshold = TimeSpan.FromMilliseconds(setting.SqlLog.ElapsedThresholdMilliseconds)
            };
            listeners.Add(new LoggingListener(provider.GetRequiredService<ILogger<LoggingListener>>(), option));
        }

        if (setting.SqlTelemetry.Enable)
        {
            listeners.Add(new OpenTelemetryListener(new OpenTelemetryListenerOption()));
        }

        return listeners.Count switch
        {
            0 => null,
            1 => listeners[0],
            _ => new ChainListener(listeners)
        };
    }

    //--------------------------------------------------------------------------------
    // Configuration
    //--------------------------------------------------------------------------------

    private static int GetEndpointPort(IConfiguration configuration, string key)
    {
        var url = configuration[key];
        if (String.IsNullOrEmpty(url) ||
            !Uri.TryCreate(url.Replace("*", "localhost", StringComparison.Ordinal).Replace("+", "localhost", StringComparison.Ordinal), UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Endpoint is not configured. key=[{key}]");
        }

        return uri.Port;
    }

    private static bool IsOtelExporterEnabled(this IConfiguration configuration) =>
        !String.IsNullOrWhiteSpace(configuration.GetOtelExporterEndpoint());

    private static string GetOtelExporterEndpoint(this IConfiguration configuration) =>
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? string.Empty;
}
