namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Template.MobileServer.Web.Components.Pages;
using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Infrastructure.Notifications;
using Template.MobileServer.Web.Services;

public sealed class NotificationPageTests : MudBlazorTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    // 一覧: 登録済みの端末が表示され、状態の報告で更新される
    [Fact]
    public void RegisteredDevicesAreRendered()
    {
        // Arrange
        var registry = new DeviceRegistry();
        registry.Register("c1", Now);
        Services.AddSingleton(registry);
        using var notifier = CreateNotifier(out _);
        Services.AddSingleton(notifier);
        AddAuthorization().SetAuthorized("admin");

        var cut = Render<NotificationPage>();

        // Assert
        Assert.Contains("接続中の端末: 1", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(["端末 ID", "機種", "OS", "電池", "ネットワーク", "最終受信", string.Empty], cut.FindAll(".devices-table th").Select(static x => x.TextContent.Trim()));

        // Act
        registry.Update("c1", new DeviceStatusMessage { DeviceId = "device-1", Model = "Pixel 9a", Platform = "Android 16", Battery = 0.5, BatteryState = "Charging", Network = "WiFi" }, Now);

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Pixel 9a", cut.Markup, StringComparison.Ordinal));
    }

    // 電池: 割合と状態。不明 (負の値) は空
    [Theory]
    [InlineData(0.5, "Charging", "50% Charging")]
    [InlineData(-1, "Unknown", "")]
    public void BatteryIsFormatted(double battery, string state, string expected)
    {
        // Arrange
        var registry = new DeviceRegistry();
        registry.Register("c1", Now);
        registry.Update("c1", new DeviceStatusMessage { DeviceId = "device-1", Model = "Pixel 9a", Platform = "Android 16", Battery = battery, BatteryState = state, Network = "WiFi" }, Now);
        Services.AddSingleton(registry);
        using var notifier = CreateNotifier(out _);
        Services.AddSingleton(notifier);
        AddAuthorization().SetAuthorized("admin");

        // Act
        var cut = Render<NotificationPage>();

        // Assert
        Assert.Equal(expected, cut.FindAll(".devices-table tbody td")[3].TextContent.Trim());
    }

    // 送信: 全端末への通知がハブへ配信される
    [Fact]
    public void SendNotifiesAllClients()
    {
        // Arrange
        var registry = new DeviceRegistry();
        registry.Register("c1", Now);
        Services.AddSingleton(registry);
        using var notifier = CreateNotifier(out var client);
        Services.AddSingleton(notifier);
        AddAuthorization().SetAuthorized("admin");

        var cut = Render<NotificationPage>();

        // Act
        cut.Find("button.send-button").Click();

        // Assert
        cut.WaitForAssertion(() => client.Received(1).Notify(Arg.Is<NotificationMessage>(static x => x.Title == "お知らせ")));
    }

    // 切断: 一覧のボタンで登録時のコールバック(ハブの Context.Abort)が呼ばれる
    [Fact]
    public void DisconnectButtonAbortsConnection()
    {
        // Arrange
        var registry = new DeviceRegistry();
        var aborted = 0;
        registry.Register("c1", Now, () => aborted++);
        Services.AddSingleton(registry);
        using var notifier = CreateNotifier(out _);
        Services.AddSingleton(notifier);
        AddAuthorization().SetAuthorized("admin");

        var cut = Render<NotificationPage>();

        // Act
        cut.Find("button.disconnect-button").Click();

        // Assert
        Assert.Equal(1, aborted);
    }

    private static MonitorNotifier CreateNotifier(out IMonitorClient client)
    {
        client = Substitute.For<IMonitorClient>();
        var clients = Substitute.For<IHubClients<IMonitorClient>>();
        clients.All.Returns(client);
        clients.Client(Arg.Any<string>()).Returns(client);
        var hub = Substitute.For<IHubContext<MonitorHub, IMonitorClient>>();
        hub.Clients.Returns(clients);
        return new MonitorNotifier(NullLogger<MonitorNotifier>.Instance, TimeProvider.System, hub, new NotificationBus());
    }
}
