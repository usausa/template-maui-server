namespace Template.MobileServer.Frontend.Shared;

using Template.MobileServer.Frontend.Shared.Shell;

public sealed partial class MainLayout : IMenuSectionCallback
{
    private RenderFragment? menu;

    private bool drawerOpen = true;

    public void SetMenu(RenderFragment? value)
    {
        menu = value;
        StateHasChanged();
    }

    private void DrawerToggle()
    {
        drawerOpen = !drawerOpen;
    }
}
