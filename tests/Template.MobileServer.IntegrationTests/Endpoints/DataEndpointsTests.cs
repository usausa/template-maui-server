namespace Template.MobileServer.Endpoints;

using System.Text;

using Template.MobileServer.Web.Endpoints;

public sealed class DataEndpointsTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public DataEndpointsTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // 作成 (201)・取得・更新 (204)・削除 (204)。削除の後は 404
    [Fact]
    public async Task CreateGetUpdateDelete()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        var name = $"it-{Guid.NewGuid():N}";

        // Act
        using var created = await client.PostAsJsonAsync(new Uri("api/data", UriKind.Relative), new DataCreateRequest { Name = name, Value = 1 }, ServerFixture.Json, TestContext.Current.CancellationToken);
        var id = (await created.Content.ReadFromJsonAsync<DataCreateResponse>(ServerFixture.Json, TestContext.Current.CancellationToken))!.Id;
        var entity = await client.GetFromJsonAsync<DataGetResponse>(new Uri($"api/data/{id}", UriKind.Relative), ServerFixture.Json, TestContext.Current.CancellationToken);
        using var updated = await client.PutAsJsonAsync(new Uri($"api/data/{id}", UriKind.Relative), new DataUpdateRequest { Name = name, Value = 2 }, ServerFixture.Json, TestContext.Current.CancellationToken);
        using var deleted = await client.DeleteAsync(new Uri($"api/data/{id}", UriKind.Relative), TestContext.Current.CancellationToken);
        using var missing = await client.GetAsync(new Uri($"api/data/{id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(entity);
        Assert.Equal(name, entity.Name);
        Assert.Equal(1, entity.Value);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedAt.Kind);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // 名前の重複は 409
    [Fact]
    public async Task DuplicateNameReturnsConflict()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        var request = new DataCreateRequest { Name = $"it-{Guid.NewGuid():N}", Value = 1 };
        using var first = await client.PostAsJsonAsync(new Uri("api/data", UriKind.Relative), request, ServerFixture.Json, TestContext.Current.CancellationToken);

        // Act
        using var second = await client.PostAsJsonAsync(new Uri("api/data", UriKind.Relative), request, ServerFixture.Json, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // 要求の JSON は厳しく読む (知らない項目は 400)
    [Fact]
    public async Task UnknownPropertyReturnsBadRequest()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        using var content = new StringContent($"{{\"name\":\"it-{Guid.NewGuid():N}\",\"value\":1,\"unknown\":true}}", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri("api/data", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
