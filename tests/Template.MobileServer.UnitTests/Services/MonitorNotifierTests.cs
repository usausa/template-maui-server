namespace Template.MobileServer.Services;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Infrastructure.Notifications;
using Template.MobileServer.Web.Services;

public sealed class MonitorNotifierTests
{
    // 中継: 通知バスへの発行が全端末へ配信される (購読は Dispose で解除)
    [Fact]
    public void BusMessageIsRelayedToAllClients()
    {
        // Arrange
        var client = Substitute.For<IMonitorClient>();
        var clients = Substitute.For<IHubClients<IMonitorClient>>();
        clients.All.Returns(client);
        var hub = Substitute.For<IHubContext<MonitorHub, IMonitorClient>>();
        hub.Clients.Returns(clients);
        var bus = new NotificationBus();
        var notifier = new MonitorNotifier(NullLogger<MonitorNotifier>.Instance, hub, TimeProvider.System, bus);

        // Act
        bus.Publish("hello");

        // Assert
        client.Received(1).Notify(Arg.Is<NotificationMessage>(static x => (x.Title == "Server") && (x.Body == "hello")));

        // Act
        notifier.Dispose();
        bus.Publish("after dispose");

        // Assert
        client.Received(1).Notify(Arg.Any<NotificationMessage>());
    }
}
