namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Template.MobileServer.Web.Components.Pages;
using Template.MobileServer.Web.Infrastructure.Chat;

public sealed class ChatPageTests : MudBlazorTestBase
{
    // 送信: ページからの発言がChatService(gRPC配信と同じ経路)へ配信される
    [Fact]
    public void SendPublishesToChatService()
    {
        // Arrange
        var chatService = new ChatService();
        Services.AddSingleton(chatService);
        Services.AddSingleton(TimeProvider.System);
        AddAuthorization().SetAuthorized("tester");

        var cut = Render<ChatPage>();

        // Act
        cut.Find("input").Input("Hello gRPC");
        cut.Find("button").Click();

        // Assert
        var entry = Assert.Single(chatService.History);
        Assert.Equal("tester", entry.User);
        Assert.Equal("Hello gRPC", entry.Text);

        // 自分の発言が即時表示される
        cut.WaitForAssertion(() => Assert.Contains("Hello gRPC", cut.Markup, StringComparison.Ordinal));
    }

    // 受信: ChatService経由の発言(gRPCクライアント側と同じ経路)がページに反映される
    [Fact]
    public void PublishedMessageIsRendered()
    {
        // Arrange
        var chatService = new ChatService();
        Services.AddSingleton(chatService);
        Services.AddSingleton(TimeProvider.System);
        AddAuthorization().SetAuthorized("tester");

        var cut = Render<ChatPage>();

        // Act
        chatService.Publish("alice", "Hello Web", DateTimeOffset.UtcNow);

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Hello Web", cut.Markup, StringComparison.Ordinal));
    }
}
