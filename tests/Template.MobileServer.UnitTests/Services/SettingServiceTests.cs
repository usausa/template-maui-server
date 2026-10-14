namespace Template.MobileServer.Services;

using Template.MobileServer.Telemetry;

public sealed class SettingServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 18, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 値は前後の空白を除いて保存し、更新日時は UTC の 7 桁の書式で保存して UTC として読む
    [Fact]
    public async Task UpdateStoresTrimmedValueAndUpdatedAtInUtc()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);

        // Act
        var rows = await storage.SettingService.UpdateAsync("OllamaModel", " model-1 ");

        // Assert
        Assert.Equal(1, rows);
        var stored = Assert.Single(await storage.SettingService.QueryAllAsync(TestContext.Current.CancellationToken));
        Assert.Equal("OllamaModel", stored.Key);
        Assert.Equal("model-1", stored.Value);
        Assert.Equal(DateTimeKind.Utc, stored.UpdatedAt.Kind);
        Assert.Equal(Now.UtcDateTime, stored.UpdatedAt);
        var text = await storage.QueryDataValueAsync(static command => command.CommandText = "SELECT UpdatedAt FROM Setting");
        Assert.Equal("2026-10-14 09:00:00.0000000", text);
    }

    // 空欄は行の削除
    [Fact]
    public async Task UpdateWithBlankDeletesRow()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        await storage.SettingService.UpdateAsync("OllamaModel", "model-1");

        // Act
        var rows = await storage.SettingService.UpdateAsync("OllamaModel", " ");

        // Assert
        Assert.Equal(1, rows);
        Assert.Empty(await storage.SettingService.QueryAllAsync(TestContext.Current.CancellationToken));
    }
}
