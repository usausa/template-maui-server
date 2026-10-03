namespace Template.MobileServer.Services;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;

public sealed class PushServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 作成: 宛先の省略は登録済みで有効な端末ごとに 1 行。無効・未登録の端末宛ては作らない
    [Fact]
    public async Task InsertTargetsEnabledDevices()
    {
        // Arrange
        await PrepareDevicesAsync();
        using var scope = storage.BeginScope(Now);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var all = await storage.PushService.InsertAsync(null, "件名", "本文", cancellationToken);
        var disabled = await storage.PushService.InsertAsync("device-2", "件名", "本文", cancellationToken);
        var unknown = await storage.PushService.InsertAsync("unknown", "件名", "本文", cancellationToken);
        var single = await storage.PushService.InsertAsync("device-1", "件名", "本文", cancellationToken);

        // Assert
        Assert.Equal(["device-1", "device-3"], all.Select(static x => x.DeviceId).Order(StringComparer.Ordinal));
        Assert.Empty(disabled);
        Assert.Empty(unknown);
        var message = Assert.Single(single);
        Assert.Equal("件名", message.Title);
        Assert.Equal("本文", message.Body);
        Assert.Equal(Now.UtcDateTime, message.CreatedAt);
        Assert.Null(message.DeliveredAt);
    }

    // 応答: 届いた日時を入れて未達から外す。2 回目と他の端末からの応答は記録しない
    [Fact]
    public async Task DeliveredLeavesPending()
    {
        // Arrange
        await PrepareDevicesAsync();
        using var scope = storage.BeginScope(Now);
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = Assert.Single(await storage.PushService.InsertAsync("device-1", "1", string.Empty, cancellationToken));
        var second = Assert.Single(await storage.PushService.InsertAsync("device-1", "2", string.Empty, cancellationToken));

        // Act / Assert: 未達は古い順
        Assert.Equal([first.Id, second.Id], (await storage.PushService.QueryPendingListAsync("device-1", cancellationToken)).Select(static x => x.Id));

        // Act / Assert: 応答
        Assert.False(await storage.PushService.UpdateDeliveredAsync("device-3", first.Id, cancellationToken));
        Assert.True(await storage.PushService.UpdateDeliveredAsync("device-1", first.Id, cancellationToken));
        Assert.False(await storage.PushService.UpdateDeliveredAsync("device-1", first.Id, cancellationToken));
        Assert.Equal([second.Id], (await storage.PushService.QueryPendingListAsync("device-1", cancellationToken)).Select(static x => x.Id));
        Assert.Equal("2026-09-28 03:00:00.0000000", await storage.QueryDataValueAsync(static x => x.CommandText = "SELECT DeliveredAt FROM PushMessage WHERE DeliveredAt IS NOT NULL"));
    }

    // 期限: 指定の日時より前に送った行は、届いた / 届いていないにかかわらず削除する
    [Fact]
    public async Task DeleteBeforeRemovesOldMessages()
    {
        // Arrange
        await PrepareDevicesAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        using (storage.BeginScope(Now.AddDays(-8)))
        {
            var delivered = Assert.Single(await storage.PushService.InsertAsync("device-1", "delivered", string.Empty, cancellationToken));
            await storage.PushService.UpdateDeliveredAsync("device-1", delivered.Id, cancellationToken);
            await storage.PushService.InsertAsync("device-1", "pending", string.Empty, cancellationToken);
        }

        using var scope = storage.BeginScope(Now);
        var current = Assert.Single(await storage.PushService.InsertAsync("device-1", "current", string.Empty, cancellationToken));

        // Act
        var rows = await storage.PushService.DeleteBeforeAsync(Now.AddDays(-7).UtcDateTime, cancellationToken);

        // Assert
        Assert.Equal(2, rows);
        Assert.Equal([current.Id], (await storage.PushService.QueryPendingListAsync("device-1", cancellationToken)).Select(static x => x.Id));
    }

    // 登録: device-1 と device-3 は有効、device-2 は無効
    private async Task PrepareDevicesAsync()
    {
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        await InsertDeviceAsync("device-1", true);
        await InsertDeviceAsync("device-2", false);
        await InsertDeviceAsync("device-3", true);
    }

    private ValueTask<DataWriteStatus> InsertDeviceAsync(string deviceId, bool enabled) =>
        storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = deviceId, Name = deviceId, IsEnabled = enabled }, TestContext.Current.CancellationToken);
}
