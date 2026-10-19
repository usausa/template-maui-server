namespace Template.MobileServer.Hubs;

using Microsoft.AspNetCore.SignalR.Client;

using Template.MobileServer.Web.Endpoints;
using Template.MobileServer.Web.Hubs;

public sealed class PushHubTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public PushHubTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task SendDeliversToConnectedDevice()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        var deviceId = await RegisterDeviceAsync(client);
        var received = new TaskCompletionSource<PushMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = CreateConnection(deviceId);
        using var subscription = connection.On<PushMessage>("Receive", x => received.TrySetResult(x));
        await connection.StartAsync(TestContext.Current.CancellationToken);

        // Act
        using var sent = await client.PostAsJsonAsync(new Uri("api/push", UriKind.Relative), new PushSendRequest { DeviceId = deviceId, Title = "Title", Body = "Body" }, ServerFixture.Json, TestContext.Current.CancellationToken);
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await connection.InvokeAsync("Acknowledge", message.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var response = await sent.Content.ReadFromJsonAsync<PushSendResponse>(ServerFixture.Json, TestContext.Current.CancellationToken);
        Assert.NotNull(response);
        Assert.Equal(1, response.Count);
        Assert.Equal("Title", message.Title);
        Assert.Equal("Body", message.Body);
    }

    [Fact]
    public async Task PendingMessageIsDeliveredOnConnect()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        var deviceId = await RegisterDeviceAsync(client);
        using var sent = await client.PostAsJsonAsync(new Uri("api/push", UriKind.Relative), new PushSendRequest { DeviceId = deviceId, Title = "Pending" }, ServerFixture.Json, TestContext.Current.CancellationToken);
        var received = new TaskCompletionSource<PushMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = CreateConnection(deviceId);
        using var subscription = connection.On<PushMessage>("Receive", x => received.TrySetResult(x));

        // Act
        await connection.StartAsync(TestContext.Current.CancellationToken);
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.Equal("Pending", message.Title);
        Assert.Equal(DateTimeKind.Utc, message.CreatedAt.Kind);
    }

    [Fact]
    public async Task SendToUnknownDeviceReturnsNotFound()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();

        // Act
        using var response = await client.PostAsJsonAsync(new Uri("api/push", UriKind.Relative), new PushSendRequest { DeviceId = $"it-{Guid.NewGuid():N}", Title = "Title" }, ServerFixture.Json, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HubConnection CreateConnection(string deviceId) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(Fixture.HttpAddress, $"hubs/push?{PushHub.DeviceIdParameter}={deviceId}"))
            .Build();

    private static async Task<string> RegisterDeviceAsync(HttpClient client)
    {
        var deviceId = $"it-{Guid.NewGuid():N}";
        using var response = await client.PutAsJsonAsync(new Uri($"api/device/{deviceId}", UriKind.Relative), new DeviceRegisterRequest { Name = "Pixel 9a" }, ServerFixture.Json, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return deviceId;
    }
}
