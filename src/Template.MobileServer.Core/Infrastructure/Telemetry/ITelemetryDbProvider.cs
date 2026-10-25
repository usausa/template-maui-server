namespace Template.MobileServer.Infrastructure.Telemetry;

public interface ITelemetryDbProvider
{
    IReadOnlyList<string> EnumerateDevices();

    ValueTask<DbConnection> OpenAsync(string terminalId, CancellationToken cancellationToken = default);

    ValueTask<DbConnection?> OpenExistingAsync(string terminalId, CancellationToken cancellationToken = default);

    bool Delete(string terminalId);
}
