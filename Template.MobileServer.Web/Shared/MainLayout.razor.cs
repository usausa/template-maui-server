namespace Template.MobileServer.Web.Shared;

public sealed partial class MainLayout : IMenuSectionCallback, IDisposable
{
#pragma warning disable CA2213
    private readonly System.Timers.Timer updateTimer = new(3600_000);
#pragma warning restore CA2213

    private ErrorBoundary? errorBoundary;

    private RenderFragment? menu;

    private bool drawerOpen = true;

    private Account account = Account.Empty;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public LoginManager LoginManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    public void Dispose() => updateTimer.Dispose();

    protected override void OnInitialized()
    {
        // Update authentication token
        updateTimer.Elapsed += (_, _) => _ = InvokeAsync(() => LoginManager.UpdateToken());
        updateTimer.Enabled = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        errorBoundary?.Recover();

        account = await AuthenticationState.ToAccount();
    }

    public void SetMenu(RenderFragment? value)
    {
        menu = value;
        StateHasChanged();
    }

    private void DrawerToggle()
    {
        drawerOpen = !drawerOpen;
    }

    private void OnLoginClick()
    {
        NavigationManager.NavigateTo("/login");
    }

    private async Task OnLogoutClick()
    {
        if (!await DialogService.ShowConfirm("Logout", "Are you sure you want to logout ?"))
        {
            return;
        }

        await LoginManager.LogoutAsync();
        NavigationManager.NavigateTo("/");
    }
}
