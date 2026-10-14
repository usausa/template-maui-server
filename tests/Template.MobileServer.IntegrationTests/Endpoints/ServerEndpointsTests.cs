namespace Template.MobileServer.Endpoints;

using Template.MobileServer.Web.Endpoints;

public sealed class ServerEndpointsTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public ServerEndpointsTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // サーバー時刻 (API の日時は UTC)
    [Fact]
    public async Task TimeReturnsUtc()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();

        // Act
        var response = await client.GetFromJsonAsync<ServerTimeResponse>(new Uri("api/server/time", UriKind.Relative), ServerFixture.Json, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(DateTimeKind.Utc, response.DateTime.Kind);
        Assert.InRange(response.DateTime, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }
}
