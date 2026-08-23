namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.FeatureManagement;

using Template.MobileServer.Web.Application;

public sealed partial class Home
{
    private bool featureEnabled;

    [Inject]
    public required IFeatureManager FeatureManager { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Feature flag example
        featureEnabled = await FeatureManager.IsEnabledAsync(FeatureFlags.CustomOption);
    }
}
