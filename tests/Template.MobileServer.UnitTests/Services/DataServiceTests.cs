namespace Template.MobileServer.Services;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;

public sealed class DataServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 18, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 作成日時は UTC の 7 桁の書式で保存し、UTC として読む
    [Fact]
    public async Task InsertStoresCreatedAtInUtc()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        var entity = new DataEntity { Name = "data-1", Value = 1 };

        // Act
        var status = await storage.DataService.InsertAsync(entity);

        // Assert
        Assert.Equal(DataWriteStatus.Success, status);
        var stored = await storage.DataService.QueryAsync(entity.Id);
        Assert.NotNull(stored);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
        Assert.Equal(Now.UtcDateTime, stored.CreatedAt);
        var text = await storage.QueryDataValueAsync(command =>
        {
            command.CommandText = "SELECT CreatedAt FROM Data WHERE Id = $id";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$id";
            parameter.Value = entity.Id;
            command.Parameters.Add(parameter);
        });
        Assert.Equal("2026-10-14 09:00:00.0000000", text);
    }
}
