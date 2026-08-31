namespace Template.MobileServer.Components.Layout;

using Bunit;

using Template.MobileServer.Web.Components.Layout;

public sealed class NavMenuTest : MudBlazorTestBase
{
    [Fact]
    public void RenderShowsNavigationLinks()
    {
        // Arrange & Act
        var cut = Render<NavMenu>();

        // Assert
        var links = cut.FindAll("a");
        Assert.Equal(5, links.Count);
    }
}
