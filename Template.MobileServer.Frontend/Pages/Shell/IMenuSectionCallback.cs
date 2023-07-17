namespace Template.MobileServer.Web.Shared.Shell;

using Microsoft.AspNetCore.Components;

public interface IMenuSectionCallback
{
    void SetMenu(RenderFragment? value);
}
