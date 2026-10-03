namespace Template.MobileServer.Services;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Services;

public sealed class PushNotifierTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    public void Dispose() => storage.Dispose();

    // 送信: 作った行ごとに宛先の端末のグループへ送る。宛先の端末が無ければ送らない
    [Fact]
    public async Task SendDeliversToDeviceGroups()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        var cancellationToken = TestContext.Current.CancellationToken;
        await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = "device-1", Name = "1", IsEnabled = true }, cancellationToken);
        await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = "device-2", Name = "2", IsEnabled = true }, cancellationToken);
        var received = new List<(string Group, PushMessage Message)>();
        var clients = Substitute.For<IHubClients<IPushClient>>();
        clients.Group(Arg.Any<string>()).Returns(x =>
        {
            var group = x.Arg<string>();
            var client = Substitute.For<IPushClient>();
            client.Receive(Arg.Do<PushMessage>(message => received.Add((group, message)))).Returns(Task.CompletedTask);
            return client;
        });
        var hub = Substitute.For<IHubContext<PushHub, IPushClient>>();
        hub.Clients.Returns(clients);
        var notifier = new PushNotifier(NullLogger<PushNotifier>.Instance, hub, storage.PushService);

        // Act
        var all = await notifier.SendAsync(null, "件名", "本文", cancellationToken);
        var unknown = await notifier.SendAsync("unknown", "件名", "本文", cancellationToken);

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Empty(unknown);
        Assert.Equal(all.Select(static x => (x.DeviceId, x.Id)), received.Select(static x => (x.Group, x.Message.Id)));
        Assert.All(received, static x => Assert.Equal("件名", x.Message.Title));
    }

    // 知らせ: 行を作ったときと届いた日時を記録したときだけ Changed
    [Fact]
    public async Task ChangedIsRaisedWhenPendingChanges()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        var cancellationToken = TestContext.Current.CancellationToken;
        await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = "device-1", Name = "1", IsEnabled = true }, cancellationToken);
        var notifier = new PushNotifier(NullLogger<PushNotifier>.Instance, Substitute.For<IHubContext<PushHub, IPushClient>>(), storage.PushService);
        var changed = 0;
        notifier.Changed += (_, _) => changed++;

        // Act / Assert: 送信 (宛先の端末が無ければ知らせない)
        var message = Assert.Single(await notifier.SendAsync("device-1", "件名", "本文", cancellationToken));
        await notifier.SendAsync("unknown", "件名", "本文", cancellationToken);
        Assert.Equal(1, changed);

        // Act / Assert: 応答 (2 回目は記録しないので知らせない)
        Assert.True(await notifier.AcknowledgeAsync("device-1", message.Id, cancellationToken));
        Assert.False(await notifier.AcknowledgeAsync("device-1", message.Id, cancellationToken));
        Assert.Equal(2, changed);
    }
}
