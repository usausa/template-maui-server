namespace Template.MobileServer.Endpoints;

using Template.MobileServer.Web.Endpoints;

public sealed class StorageEndpointsTests : IClassFixture<ServerFixture>
{
    private ServerFixture Fixture { get; }

    public StorageEndpointsTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }

    // アップロード・一覧・ダウンロード・フォルダーごとの削除
    [Fact]
    public async Task UploadListDownloadDelete()
    {
        // Arrange
        using var client = Fixture.CreateHttpClient();
        var folder = $"it-{Guid.NewGuid():N}";
        using var content = new ByteArrayContent("hello"u8.ToArray());

        // Act
        using var uploaded = await client.PostAsync(new Uri($"api/storage/{folder}/file.txt", UriKind.Relative), content, TestContext.Current.CancellationToken);
        var list = await client.GetFromJsonAsync<StorageListResponse>(new Uri($"api/storage/{folder}/", UriKind.Relative), ServerFixture.Json, TestContext.Current.CancellationToken);
        var text = await client.GetStringAsync(new Uri($"api/storage/{folder}/file.txt", UriKind.Relative), TestContext.Current.CancellationToken);
        using var deleted = await client.DeleteAsync(new Uri($"api/storage/{folder}", UriKind.Relative), TestContext.Current.CancellationToken);
        using var missing = await client.GetAsync(new Uri($"api/storage/{folder}/file.txt", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        Assert.NotNull(list);
        var entry = Assert.Single(list.Entries);
        Assert.Equal("file.txt", entry.Name);
        Assert.False(entry.Directory);
        Assert.Equal("hello", text);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
