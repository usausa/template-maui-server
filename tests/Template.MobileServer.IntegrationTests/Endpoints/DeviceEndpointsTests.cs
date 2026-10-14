namespace Template.MobileServer.Endpoints;

using Template.MobileServer.Web.Endpoints;

public sealed class DeviceEndpointsTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public DeviceEndpointsTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // 未登録なら登録して 201、登録済みなら名前を更新して 200
    [Fact]
    public async Task RegisterCreatesThenUpdatesName()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        var path = new Uri($"api/device/it-{Guid.NewGuid():N}", UriKind.Relative);

        // Act
        using var created = await client.PutAsJsonAsync(path, new DeviceRegisterRequest { Name = "Pixel 9a" }, ServerFixture.Json, TestContext.Current.CancellationToken);
        using var updated = await client.PutAsJsonAsync(path, new DeviceRegisterRequest { Name = "Renamed" }, ServerFixture.Json, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var response = await updated.Content.ReadFromJsonAsync<DeviceRegisterResponse>(ServerFixture.Json, TestContext.Current.CancellationToken);
        Assert.NotNull(response);
        Assert.Equal("Renamed", response.Name);
        Assert.True(response.IsEnabled);
    }

    // 端末 ID の形式が違えば 400
    [Fact]
    public async Task InvalidDeviceIdReturnsBadRequest()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();

        // Act
        using var response = await client.PutAsJsonAsync(new Uri("api/device/invalid.id", UriKind.Relative), new DeviceRegisterRequest { Name = "Pixel 9a" }, ServerFixture.Json, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
