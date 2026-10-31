namespace Template.MobileServer.Services;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;

public sealed class DeviceServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    [Fact]
    public async Task InsertRejectsDuplicate()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);

        // Act
        var first = await storage.DeviceService.InsertAsync(new DeviceEntity { TerminalId = "device-1", Name = "Pixel 9a", IsEnabled = true });
        var second = await storage.DeviceService.InsertAsync(new DeviceEntity { TerminalId = "device-1", Name = "other", IsEnabled = true });

        // Assert
        Assert.Equal(DataWriteStatus.Success, first);
        Assert.Equal(DataWriteStatus.Duplicate, second);
        var device = await storage.DeviceService.QueryAsync("device-1");
        Assert.NotNull(device);
        Assert.Equal("Pixel 9a", device.Name);
        Assert.Equal(Now.UtcDateTime, device.RegisteredAt);
    }

    [Fact]
    public async Task UpdateAndDelete()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        await storage.DeviceService.InsertAsync(new DeviceEntity { TerminalId = "device-1", Name = "Pixel 9a", IsEnabled = true });

        // Act / Assert
        var updated = await storage.DeviceService.UpdateAsync(new DeviceEntity { TerminalId = "device-1", Name = "受付", GroupName = "1F", Note = "memo", IsEnabled = false });
        Assert.NotNull(updated);
        Assert.Equal("受付", updated.Name);
        Assert.Equal("1F", updated.GroupName);
        Assert.False(updated.IsEnabled);
        Assert.Null(await storage.DeviceService.UpdateAsync(new DeviceEntity { TerminalId = "unknown", Name = "x" }));

        // Act / Assert
        Assert.Equal(DataWriteStatus.Success, await storage.DeviceService.DeleteAsync("device-1"));
        Assert.Equal(DataWriteStatus.NotFound, await storage.DeviceService.DeleteAsync("device-1"));
    }

    [Fact]
    public async Task DeleteRemovesPushMessages()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        await storage.DeviceService.InsertAsync(new DeviceEntity { TerminalId = "device-1", Name = "Pixel 9a", IsEnabled = true });
        await storage.PushService.InsertAsync("device-1", "件名", "本文");

        // Act
        var status = await storage.DeviceService.DeleteAsync("device-1");

        // Assert
        Assert.Equal(DataWriteStatus.Success, status);
        Assert.Equal(0L, await storage.QueryDataValueAsync(static x => x.CommandText = "SELECT COUNT(*) FROM PushMessage"));
    }
}
