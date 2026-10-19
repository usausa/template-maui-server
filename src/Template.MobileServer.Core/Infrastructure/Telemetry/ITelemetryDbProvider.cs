namespace Template.MobileServer.Infrastructure.Telemetry;

public interface ITelemetryDbProvider
{
    IReadOnlyList<string> EnumerateDevices();

    ValueTask<DbConnection> OpenAsync(string deviceId, CancellationToken cancellationToken = default);

    ValueTask<DbConnection?> OpenExistingAsync(string deviceId, CancellationToken cancellationToken = default);

    bool Delete(string deviceId);
}
