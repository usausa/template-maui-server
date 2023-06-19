namespace Template.MobileServer.Web.Shared.Shell;

using Microsoft.AspNetCore.Components;

public sealed class MenuSection : ComponentBase, IDisposable
{
    [CascadingParameter]
    public IMenuSectionCallback Callback { get; set; } = default!;

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    protected override void OnInitialized()
    {
        Callback.SetMenu(ChildContent);
    }

    public void Dispose()
    {
        Callback.SetMenu(null);
    }
}
