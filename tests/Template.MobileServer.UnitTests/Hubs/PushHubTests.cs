namespace Template.MobileServer.Hubs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Template.MobileServer.Models.Entity;
using Template.MobileServer.Telemetry;
using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Services;

public sealed class PushHubTests : IDisposable
{
    private const string ConnectionId = "connection-1";

    private const string DeviceId = "device-1";

    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.FromHours(9));

    private readonly TelemetryTestStorage storage = new();

    private readonly HubCallerContext context = Substitute.For<HubCallerContext>();

    private readonly IGroupManager groups = Substitute.For<IGroupManager>();

    private readonly List<PushMessage> received = [];

    public void Dispose() => storage.Dispose();

    // 接続: 端末 ID のグループに入れ、未達の通知を古い順に送る。応答した通知は未達から外れる
    [Fact]
    public async Task ConnectSendsPendingAndAcknowledgeRecordsDelivered()
    {
        // Arrange
        await storage.PrepareDatabaseAsync();
        using var scope = storage.BeginScope(Now);
        var cancellationToken = TestContext.Current.CancellationToken;
        await storage.DeviceService.InsertAsync(new DeviceEntity { DeviceId = DeviceId, Name = "Pixel 9a", IsEnabled = true }, cancellationToken);
        var first = Assert.Single(await storage.PushService.InsertAsync(DeviceId, "1", "本文", cancellationToken));
        var second = Assert.Single(await storage.PushService.InsertAsync(DeviceId, "2", "本文", cancellationToken));
        using var hub = CreateHub(DeviceId);

        // Act / Assert: 接続
        await hub.OnConnectedAsync();
        await groups.Received(1).AddToGroupAsync(ConnectionId, DeviceId, Arg.Any<CancellationToken>());
        Assert.Equal([first.Id, second.Id], received.Select(static x => x.Id));
        Assert.Equal(["1", "2"], received.Select(static x => x.Title));
        context.DidNotReceive().Abort();

        // Act / Assert: 応答
        await hub.Acknowledge(first.Id);
        Assert.Equal([second.Id], (await storage.PushService.QueryPendingListAsync(DeviceId, cancellationToken)).Select(static x => x.Id));
    }

    // 端末 ID の形式が違えば切る
    [Theory]
    [InlineData("")]
    [InlineData("bad id")]
    public async Task ConnectRejectsInvalidDeviceId(string deviceId)
    {
        // Arrange
        using var hub = CreateHub(deviceId);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        context.Received(1).Abort();
        await groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!, TestContext.Current.CancellationToken);
        Assert.Empty(received);
    }

    // 接続の URL のクエリで端末 ID を渡す
    private PushHub CreateHub(string deviceId)
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { QueryString = QueryString.Create(PushHub.DeviceIdParameter, deviceId) }
        };
        var feature = Substitute.For<IHttpContextFeature>();
        feature.HttpContext.Returns(httpContext);
        var features = new FeatureCollection();
        features.Set(feature);
        context.ConnectionId.Returns(ConnectionId);
        context.Features.Returns(features);
        context.Items.Returns(new Dictionary<object, object?>());

        var caller = Substitute.For<IPushClient>();
        caller.Receive(Arg.Do<PushMessage>(received.Add)).Returns(Task.CompletedTask);
        var clients = Substitute.For<IHubCallerClients<IPushClient>>();
        clients.Caller.Returns(caller);

        var notifier = new PushNotifier(NullLogger<PushNotifier>.Instance, Substitute.For<IHubContext<PushHub, IPushClient>>(), storage.PushService);
        return new PushHub(NullLogger<PushHub>.Instance, storage.PushService, notifier)
        {
            Context = context,
            Clients = clients,
            Groups = groups
        };
    }
}
