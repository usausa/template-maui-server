namespace Template.MobileServer.Services;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;

public sealed class DeviceServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 登録: 登録日時 (UTC) を補い、登録済みの端末 ID は Duplicate
    [Fact]
    public async Task InsertRejectsDuplicate()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);

        // Act
        var first = await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = "device-1", Name = "Pixel 9a", IsEnabled = true }, TestContext.Current.CancellationToken);
        var second = await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = "device-1", Name = "other", IsEnabled = true }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(DataWriteStatus.Success, first);
        Assert.Equal(DataWriteStatus.Duplicate, second);
        var device = await storage.DeviceService.QueryAsync("device-1", TestContext.Current.CancellationToken);
        Assert.NotNull(device);
        Assert.Equal("Pixel 9a", device.Name);
        Assert.Equal(Now.UtcDateTime, device.RegisteredAt);
    }

    // 更新と削除: 更新後の行を返し、無い端末は null / NotFound
    [Fact]
    public async Task UpdateAndDelete()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = "device-1", Name = "Pixel 9a", IsEnabled = true }, TestContext.Current.CancellationToken);

        // Act / Assert: 更新
        var updated = await storage.DeviceService.UpdateAsync(new DeviceEntity { DeviceId = "device-1", Name = "受付", GroupName = "1F", Note = "memo", IsEnabled = false }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("受付", updated.Name);
        Assert.Equal("1F", updated.GroupName);
        Assert.False(updated.IsEnabled);
        Assert.Null(await storage.DeviceService.UpdateAsync(new DeviceEntity { DeviceId = "unknown", Name = "x" }, TestContext.Current.CancellationToken));

        // Act / Assert: 削除
        Assert.Equal(DataWriteStatus.Success, await storage.DeviceService.DeleteAsync("device-1", TestContext.Current.CancellationToken));
        Assert.Equal(DataWriteStatus.NotFound, await storage.DeviceService.DeleteAsync("device-1", TestContext.Current.CancellationToken));
    }
}
