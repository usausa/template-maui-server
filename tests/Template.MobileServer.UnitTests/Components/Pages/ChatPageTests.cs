namespace Template.MobileServer.Components.Pages;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Template.MobileServer.Web.Components.Pages;
using Template.MobileServer.Web.Services;

public sealed class ChatPageTests : MudBlazorTestBase
{
    [Fact]
    public void SendPublishesToChatService()
    {
        // Arrange
        var chatService = new ChatService();
        Services.AddSingleton(chatService);

        var cut = Render<ChatPage>();

        // Act
        cut.FindAll("input")[1].Input("Hello gRPC");
        cut.Find("button").Click();

        // Assert
        var entry = Assert.Single(chatService.History);
        Assert.Equal("web", entry.User);
        Assert.Equal("Hello gRPC", entry.Text);

        cut.WaitForAssertion(() => Assert.Contains("Hello gRPC", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void PublishedMessageIsRendered()
    {
        // Arrange
        var chatService = new ChatService();
        Services.AddSingleton(chatService);

        var cut = Render<ChatPage>();

        // Act
        chatService.Publish("alice", "Hello Web", DateTimeOffset.UtcNow);

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Hello Web", cut.Markup, StringComparison.Ordinal));
    }
}
