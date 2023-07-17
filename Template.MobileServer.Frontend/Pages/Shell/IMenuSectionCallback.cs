namespace Template.MobileServer.Frontend.Pages.Shell;

using Microsoft.AspNetCore.Components;

public interface IMenuSectionCallback
{
    void SetMenu(RenderFragment? value);
}
