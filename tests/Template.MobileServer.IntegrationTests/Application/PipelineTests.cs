namespace Template.MobileServer.Application;

public sealed class PipelineTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public PipelineTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // 画面の未定義の URL は「ページが見つかりません」の 404 (Server ヘッダーは出さない)
    [Fact]
    public async Task UnknownPageReturnsNotFoundPage()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();

        // Act
        using var response = await client.GetAsync(new Uri("no-such-page", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("ページが見つかりません", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.False(response.Headers.Contains("Server"));
    }

    // API の未定義の URL は ProblemDetails の 404
    [Fact]
    public async Task UnknownApiReturnsProblemDetails()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();

        // Act
        using var response = await client.GetAsync(new Uri("api/no-such-api", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ヘルスチェック (全体と生存)
    [Fact]
    public async Task HealthChecksAreHealthy()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();

        // Act
        var health = await client.GetStringAsync(new Uri("health", UriKind.Relative), TestContext.Current.CancellationToken);
        var alive = await client.GetStringAsync(new Uri("alive", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Healthy", health);
        Assert.Equal("Healthy", alive);
    }
}
