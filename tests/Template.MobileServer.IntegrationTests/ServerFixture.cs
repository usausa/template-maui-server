namespace Template.MobileServer;

using System.Net.Sockets;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

// 実際のポートで起動するサーバー (gRPC と OTLP はポートで受け口を限定しているため)。データ・ファイル・ログは一時フォルダーに置く
public sealed class ServerFixture : WebApplicationFactory<Program>
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "template-integration-tests", Guid.NewGuid().ToString("N"));

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web);

    public int HttpPort { get; } = GetFreePort();

    public int GrpcPort { get; } = GetFreePort();

    public int OtelPort { get; } = GetFreePort();

    public int OtelHttpPort { get; } = GetFreePort();

    public Uri HttpAddress => new($"http://127.0.0.1:{HttpPort}/");

    public Uri GrpcAddress => new($"http://127.0.0.1:{GrpcPort}/");

    public Uri OtelAddress => new($"http://127.0.0.1:{OtelPort}/");

    public Uri OtelHttpAddress => new($"http://127.0.0.1:{OtelHttpPort}/");

    public ServerFixture()
    {
        Directory.CreateDirectory(root);
        UseKestrel();
        StartServer();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Kestrel:Endpoints:Http:Url", $"http://127.0.0.1:{HttpPort}");
        builder.UseSetting("Kestrel:Endpoints:Grpc:Url", $"http://127.0.0.1:{GrpcPort}");
        builder.UseSetting("Kestrel:Endpoints:Otel:Url", $"http://127.0.0.1:{OtelPort}");
        builder.UseSetting("Kestrel:Endpoints:OtelHttp:Url", $"http://127.0.0.1:{OtelHttpPort}");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={Path.Combine(root, "data.db")};Cache=Shared;Pooling=True");
        builder.UseSetting("Storage:Root", Path.Combine(root, "storage"));
        builder.UseSetting("TelemetryStorage:Root", Path.Combine(root, "telemetry"));
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Serilog:WriteTo:0:Args:path", Path.Combine(root, "log", "server_.log"));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, true);
        }
    }

    public HttpClient CreateHttpClient() => new() { BaseAddress = HttpAddress };

    public HttpClient CreateOtelHttpClient() => new() { BaseAddress = OtelHttpAddress };

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
