namespace Template.MobileServer.Frontend.Shared.Shell;

public sealed class MenuSection : ComponentBase, IDisposable
{
    [CascadingParameter]
    public required IMenuSectionCallback Callback { get; set; } = default!;

    [Parameter]
    public required RenderFragment ChildContent { get; set; } = default!;

    protected override void OnInitialized()
    {
        Callback.SetMenu(ChildContent);
    }

    public void Dispose()
    {
        Callback.SetMenu(null);
    }
}
