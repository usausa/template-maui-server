namespace Template.MobileServer.Hubs;

using Microsoft.AspNetCore.SignalR.Client;

using Template.MobileServer.Web.Hubs;

public sealed class MonitorHubTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public MonitorHubTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // 接続するとサーバーの状態が届く (接続数に自分を含む)
    [Fact]
    public async Task ServerStatusIsBroadcast()
    {
        // Arrange
        var received = new TaskCompletionSource<ServerStatusMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Fixture.HttpAddress, "hubs/monitor"))
            .Build();
        using var subscription = connection.On<ServerStatusMessage>("ServerStatus", x => received.TrySetResult(x));

        // Act
        await connection.StartAsync(TestContext.Current.CancellationToken);
        var status = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        Assert.InRange(status.Connections, 1, Int32.MaxValue);
    }
}
