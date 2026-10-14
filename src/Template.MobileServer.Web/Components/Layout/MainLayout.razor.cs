namespace Template.MobileServer.Web.Components.Layout;

using Microsoft.AspNetCore.Components.Web;

public sealed partial class MainLayout
{
    private ErrorBoundary? errorBoundary;

    private bool drawerOpen = true;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnParametersSet() => errorBoundary?.Recover();

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void ToggleDrawer() => drawerOpen = !drawerOpen;

    private void Recover() => errorBoundary?.Recover();
}
