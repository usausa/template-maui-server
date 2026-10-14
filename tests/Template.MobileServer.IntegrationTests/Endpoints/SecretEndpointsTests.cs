namespace Template.MobileServer.Endpoints;

using System.Net.Http.Headers;

using Template.MobileServer.Web.Endpoints;

public sealed class SecretEndpointsTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public SecretEndpointsTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // トークンが無ければ 401 (ProblemDetails)、ログインの JWT を付ければ呼べる
    [Fact]
    public async Task MessageRequiresToken()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        using var login = await client.PostAsJsonAsync(new Uri("api/account/login", UriKind.Relative), new AccountLoginRequest { Id = "user-1" }, ServerFixture.Json, TestContext.Current.CancellationToken);
        var token = (await login.Content.ReadFromJsonAsync<AccountLoginResponse>(ServerFixture.Json, TestContext.Current.CancellationToken))!.Token;

        // Act
        using var anonymous = await client.GetAsync(new Uri("api/secret/message", UriKind.Relative), TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("api/secret/message", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var authorized = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("application/problem+json", anonymous.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }
}
